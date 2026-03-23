using FlashBank.Shared.Enums;

namespace FlashBank.Shared.Events;

public record TransactionCreated(
    Guid TransactionId,
    Guid AccountId,
    decimal Amount,
    TransactionType Type,
    DateTime CreatedAt
);
