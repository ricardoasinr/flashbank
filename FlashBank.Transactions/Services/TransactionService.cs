using FlashBank.Shared.Enums;
using FlashBank.Shared.Events;
using FlashBank.Transactions.Data;
using FlashBank.Transactions.DTOs;
using FlashBank.Transactions.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace FlashBank.Transactions.Services;

public class TransactionService : ITransactionService
{
    private readonly TransactionDbContext _db;
    private readonly IBus _bus;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(TransactionDbContext db, IBus bus, ILogger<TransactionService> logger)
    {
        _db     = db;
        _bus    = bus;
        _logger = logger;
    }

    public async Task<Transaction> CreateAsync(CreateTransactionRequest request, CancellationToken ct = default)
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

        return transaction;
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
    }
}
