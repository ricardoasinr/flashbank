using FlashBank.Shared.Enums;

namespace FlashBank.Transactions.DTOs;

public record CreateTransactionRequest(
    Guid AccountId,
    decimal Amount,
    TransactionType Type
);
