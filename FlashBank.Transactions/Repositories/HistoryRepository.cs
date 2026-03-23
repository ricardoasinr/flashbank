using FlashBank.Transactions.Documents;
using MongoDB.Driver;

namespace FlashBank.Transactions.Repositories;

public class HistoryRepository : IHistoryRepository
{
    private readonly IMongoCollection<TransactionHistoryDocument> _collection;
    private readonly ILogger<HistoryRepository> _logger;

    public HistoryRepository(IMongoDatabase database, IConfiguration configuration, ILogger<HistoryRepository> logger)
    {
        var collectionName = configuration["MongoDB:Collection"] ?? "transaction-history";
        _collection = database.GetCollection<TransactionHistoryDocument>(collectionName);
        _logger = logger;
    }

    public async Task InsertAsync(TransactionHistoryDocument document, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(document, cancellationToken: cancellationToken);

        _logger.LogInformation(
            "[HistoryRepository] Registro insertado → TransactionId: {TransactionId} | Status: {Status} | Type: {Type} | Amount: {Amount}",
            document.TransactionId, document.Status, document.Type, document.Amount);
    }
}
