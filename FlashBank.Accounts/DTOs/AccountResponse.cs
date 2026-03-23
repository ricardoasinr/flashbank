namespace FlashBank.Accounts.DTOs;

public record AccountResponse(
    Guid Id,
    string Name,
    decimal Balance,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
