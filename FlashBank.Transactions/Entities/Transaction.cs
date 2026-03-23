using FlashBank.Shared.Enums;

namespace FlashBank.Transactions.Entities;

public class Transaction
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
