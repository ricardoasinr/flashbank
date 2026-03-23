using FlashBank.Accounts.Data;
using FlashBank.Accounts.DTOs;
using FlashBank.Accounts.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlashBank.Accounts.Services;

public class AccountService : IAccountService
{
    private readonly AccountDbContext _db;
    private readonly ILogger<AccountService> _logger;

    public AccountService(AccountDbContext db, ILogger<AccountService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task<IEnumerable<AccountResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Accounts
            .OrderBy(a => a.Name)
            .Select(a => new AccountResponse(a.Id, a.Name, a.Balance, a.CreatedAt, a.UpdatedAt))
            .ToListAsync(ct);
    }

    public async Task<AccountResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var account = await _db.Accounts.FindAsync(new object[] { id }, ct);
        return account is null ? null : ToResponse(account);
    }

    public async Task<AccountResponse> CreateAsync(CreateAccountRequest request, CancellationToken ct = default)
    {
        var account = new Account
        {
            Id        = Guid.NewGuid(),
            Name      = request.Name.Trim(),
            Balance   = request.InitialBalance,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "[AccountService] Cuenta creada → Id: {Id} | Name: {Name}",
            account.Id, account.Name);

        return ToResponse(account);
    }

    private static AccountResponse ToResponse(Account account) =>
        new(account.Id, account.Name, account.Balance, account.CreatedAt, account.UpdatedAt);
}
