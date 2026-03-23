using FlashBank.Shared.Enums;

namespace FlashBank.Transactions.DTOs;

public record TransactionResponse(
    Guid Id,
    Guid AccountId,
    decimal Amount,
    TransactionType Type,
    TransactionStatus Status,
    DateTime CreatedAt
);
