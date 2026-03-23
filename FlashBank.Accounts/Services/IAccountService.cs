using FlashBank.Accounts.DTOs;

namespace FlashBank.Accounts.Services;

public interface IAccountService
{
    Task<IEnumerable<AccountResponse>> GetAllAsync(CancellationToken ct = default);
    Task<AccountResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AccountResponse> CreateAsync(CreateAccountRequest request, CancellationToken ct = default);
}
