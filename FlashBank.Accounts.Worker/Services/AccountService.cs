using FlashBank.Accounts.Worker.Data;
using FlashBank.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace FlashBank.Accounts.Worker.Services;

public class AccountService : IAccountService
{
    private readonly AccountDbContext _db;
    private readonly ILogger<AccountService> _logger;

    public AccountService(AccountDbContext db, ILogger<AccountService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task UpdateBalanceAsync(Guid accountId, decimal amount, TransactionType type, CancellationToken cancellationToken = default)
    {
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);

        if (account is null)
        {
            _logger.LogWarning(
                "[AccountService] Cuenta no encontrada → AccountId: {AccountId}",
                accountId);
            throw new InvalidOperationException($"Cuenta no encontrada: {accountId}");
        }

        if (type == TransactionType.Deposit)
        {
            account.Balance += amount;
        }
        else
        {
            if (account.Balance < amount)
            {
                _logger.LogWarning(
                    "[AccountService] Fondos insuficientes → AccountId: {AccountId} | Balance: {Balance} | Amount: {Amount}",
                    accountId, account.Balance, amount);
                throw new InvalidOperationException(
                    $"Fondos insuficientes. Balance: {account.Balance}, Retiro solicitado: {amount}");
            }

            account.Balance -= amount;
        }

        account.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "[AccountService] Balance actualizado → AccountId: {AccountId} | Tipo: {Type} | Monto: {Amount} | Nuevo balance: {Balance}",
            accountId, type, amount, account.Balance);
    }
}
