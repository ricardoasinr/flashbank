using FlashBank.Shared.Enums;
using FlashBank.Transactions.DTOs;

namespace FlashBank.Transactions.Services;

public interface ITransactionService
{
    Task<TransactionResponse> CreateAsync(CreateTransactionRequest request, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid transactionId, TransactionStatus newStatus, CancellationToken ct = default);
}
