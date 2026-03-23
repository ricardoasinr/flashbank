using FlashBank.Shared.Enums;
using FlashBank.Transactions.DTOs;
using FlashBank.Transactions.Entities;

namespace FlashBank.Transactions.Services;

public interface ITransactionService
{
    Task<Transaction> CreateAsync(CreateTransactionRequest request, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid transactionId, TransactionStatus newStatus, CancellationToken ct = default);
}
