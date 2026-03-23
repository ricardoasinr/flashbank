using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FlashBank.Transactions.Documents;

public class TransactionHistoryDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid TransactionId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid AccountId { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Amount { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; }
}
