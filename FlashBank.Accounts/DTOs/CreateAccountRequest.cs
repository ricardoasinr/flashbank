namespace FlashBank.Accounts.DTOs;

public record CreateAccountRequest(string Name, decimal InitialBalance = 0m);
