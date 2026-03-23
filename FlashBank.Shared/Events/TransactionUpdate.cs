using FlashBank.Shared.Enums;

namespace FlashBank.Shared.Events;

public record TransactionUpdate(
    Guid TransactionId,
    Guid AccountId,
    TransactionStatus NewStatus,
    string? Message = null
);
