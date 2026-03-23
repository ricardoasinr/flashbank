using FlashBank.Shared.Enums;

namespace FlashBank.Shared.Events;

/// <summary>
/// Constructor sin parámetros requerido por MassTransit al deserializar desde SQS.
/// </summary>
public record TransactionUpdate
{
    public Guid TransactionId { get; init; }
    public Guid AccountId { get; init; }
    public TransactionStatus NewStatus { get; init; }
    public string? Message { get; init; }

    public TransactionUpdate()
    {
    }

    public TransactionUpdate(Guid transactionId, Guid accountId, TransactionStatus newStatus, string? message = null)
    {
        TransactionId = transactionId;
        AccountId     = accountId;
        NewStatus     = newStatus;
        Message       = message;
    }
}
