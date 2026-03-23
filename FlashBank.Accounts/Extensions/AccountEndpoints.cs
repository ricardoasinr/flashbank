using FlashBank.Accounts.DTOs;
using FlashBank.Accounts.Services;

namespace FlashBank.Accounts.Extensions;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/accounts").WithOpenApi();

        group.MapGet("/",          GetAll)    .WithName("GetAccounts");
        group.MapGet("/{id:guid}", GetById)   .WithName("GetAccountById");
        group.MapPost("/",         Create)    .WithName("CreateAccount");

        return app;
    }

    private static async Task<IResult> GetAll(IAccountService service, CancellationToken ct)
    {
        var accounts = await service.GetAllAsync(ct);
        return Results.Ok(accounts);
    }

    private static async Task<IResult> GetById(Guid id, IAccountService service, CancellationToken ct)
    {
        var account = await service.GetByIdAsync(id, ct);
        return account is null
            ? Results.NotFound(new { error = $"Cuenta no encontrada: {id}" })
            : Results.Ok(account);
    }

    private static async Task<IResult> Create(
        CreateAccountRequest request,
        IAccountService service,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "El nombre de la cuenta es obligatorio." });

        if (request.InitialBalance < 0)
            return Results.BadRequest(new { error = "El balance inicial no puede ser negativo." });

        var account = await service.CreateAsync(request, ct);
        return Results.Created($"/accounts/{account.Id}", account);
    }
}
