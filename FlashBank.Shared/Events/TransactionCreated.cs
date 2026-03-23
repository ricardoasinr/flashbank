using FlashBank.Shared.Enums;

namespace FlashBank.Shared.Events;

/// <summary>
/// Constructor sin parámetros requerido por MassTransit al deserializar desde SQS.
/// </summary>
public record TransactionCreated
{
    public Guid TransactionId { get; init; }
    public Guid AccountId { get; init; }
    public decimal Amount { get; init; }
    public TransactionType Type { get; init; }
    public DateTime CreatedAt { get; init; }

    public TransactionCreated()
    {
    }

    public TransactionCreated(Guid transactionId, Guid accountId, decimal amount, TransactionType type, DateTime createdAt)
    {
        TransactionId = transactionId;
        AccountId     = accountId;
        Amount        = amount;
        Type          = type;
        CreatedAt     = createdAt;
    }
}
