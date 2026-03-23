using FlashBank.Shared.Enums;

namespace FlashBank.Accounts.Worker.Services;

/// <summary>
/// Stub: ejecuta la actualización de saldo en la DB SQL Accounts.
/// Implementación real pendiente con EF Core + PostgreSQL.
/// </summary>
public class AccountService : IAccountService
{
    private readonly ILogger<AccountService> _logger;

    public AccountService(ILogger<AccountService> logger)
    {
        _logger = logger;
    }

    public Task UpdateBalanceAsync(Guid accountId, decimal amount, TransactionType type, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[AccountService] UpdateBalance → AccountId: {AccountId} | Amount: {Amount} | Type: {Type}",
            accountId, amount, type);

        // TODO: implementar con EF Core + PostgreSQL (flashbank_accounts)
        return Task.CompletedTask;
    }
}
