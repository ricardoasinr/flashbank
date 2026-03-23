using FlashBank.Transactions.Documents;

namespace FlashBank.Transactions.Repositories;

public interface IHistoryRepository
{
    Task InsertAsync(TransactionHistoryDocument document, CancellationToken cancellationToken = default);
}
