using FlashBank.Shared.Enums;
using FlashBank.Shared.Events;
using FlashBank.Transactions.Data;
using FlashBank.Transactions.Documents;
using FlashBank.Transactions.DTOs;
using FlashBank.Transactions.Entities;
using FlashBank.Transactions.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace FlashBank.Transactions.Services;

public class TransactionService : ITransactionService
{
    private readonly TransactionDbContext _db;
    private readonly IBus _bus;
    private readonly IHistoryRepository _history;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(TransactionDbContext db, IBus bus, IHistoryRepository history, ILogger<TransactionService> logger)
    {
        _db      = db;
        _bus     = bus;
        _history = history;
        _logger  = logger;
    }

    public async Task<TransactionResponse> CreateAsync(CreateTransactionRequest request, CancellationToken ct = default)
    {
        var transaction = new Transaction
        {
            Id        = Guid.NewGuid(),
            AccountId = request.AccountId,
            Amount    = request.Amount,
            Type      = request.Type,
            Status    = TransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "[TransactionService] Transacción creada → Id: {Id} | AccountId: {AccountId} | Amount: {Amount} | Type: {Type}",
            transaction.Id, transaction.AccountId, transaction.Amount, transaction.Type);

        var evt = new TransactionCreated(
            transaction.Id,
            transaction.AccountId,
            transaction.Amount,
            transaction.Type,
            transaction.CreatedAt);

        await _bus.Publish(evt, ct);

        _logger.LogInformation(
            "[TransactionService] Evento TransactionCreated publicado → TransactionId: {Id}",
            transaction.Id);

        return ToResponse(transaction);
    }

    public async Task UpdateStatusAsync(Guid transactionId, TransactionStatus newStatus, CancellationToken ct = default)
    {
        var transaction = await _db.Transactions
            .FirstOrDefaultAsync(t => t.Id == transactionId, ct);

        if (transaction is null)
        {
            _logger.LogWarning(
                "[TransactionService] Transacción no encontrada → TransactionId: {TransactionId}",
                transactionId);
            return;
        }

        transaction.Status = newStatus;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "[TransactionService] Status actualizado → TransactionId: {TransactionId} | Status: {Status}",
            transaction.Id, transaction.Status);

        var historyDoc = new TransactionHistoryDocument
        {
            TransactionId = transaction.Id,
            AccountId     = transaction.AccountId,
            Amount        = transaction.Amount,
            Type          = transaction.Type.ToString(),
            Status        = transaction.Status.ToString(),
            OccurredAt    = DateTime.UtcNow
        };

        await _history.InsertAsync(historyDoc, ct);

        _logger.LogInformation(
            "[TransactionService] Historial registrado → TransactionId: {TransactionId} | Status: {Status}",
            transaction.Id, transaction.Status);
    }

    private static TransactionResponse ToResponse(Transaction t) =>
        new(t.Id, t.AccountId, t.Amount, t.Type, t.Status, t.CreatedAt);
}
