using FlashBank.Shared.Enums;

namespace FlashBank.Accounts.Worker.Services;

public interface IAccountService
{
    Task UpdateBalanceAsync(Guid accountId, decimal amount, TransactionType type, CancellationToken cancellationToken = default);
}
