using FlashBank.Transactions.DTOs;
using FlashBank.Transactions.Services;

namespace FlashBank.Transactions.Extensions;

public static class TransactionEndpoints
{
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/transaction",  CreateTransaction).WithName("CreateTransactionSingular").WithOpenApi();
        app.MapPost("/transactions", CreateTransaction).WithName("CreateTransaction").WithOpenApi();

        return app;
    }

    private static async Task<IResult> CreateTransaction(
        CreateTransactionRequest request,
        ITransactionService transactionService,
        CancellationToken ct)
    {
        if (request.Amount <= 0)
            return Results.BadRequest(new { error = "El monto debe ser mayor que cero." });

        if (request.AccountId == Guid.Empty)
            return Results.BadRequest(new { error = "AccountId inválido." });

        var transaction = await transactionService.CreateAsync(request, ct);
        return Results.Created($"/transactions/{transaction.Id}", transaction);
    }
}
