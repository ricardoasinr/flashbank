using Amazon.SimpleNotificationService;
using Amazon.SQS;
using FlashBank.Transactions.Consumers;
using FlashBank.Transactions.Data;
using FlashBank.Transactions.DTOs;
using FlashBank.Transactions.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core — PostgreSQL (flashbank_transactions)
builder.Services.AddDbContext<TransactionDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TransactionsDb")));

// Servicios de negocio
builder.Services.AddScoped<ITransactionService, TransactionService>();

// MassTransit — Amazon SQS con LocalStack
var awsSection = builder.Configuration.GetSection("AWS");
var serviceUrl = awsSection["ServiceURL"] ?? "http://localhost:4566";
var accessKey  = awsSection["AccessKey"]  ?? "test";
var secretKey  = awsSection["SecretKey"]  ?? "test";
var region     = awsSection["Region"]     ?? "us-east-1";

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<TransactionConsumer>();

    x.UsingAmazonSqs((ctx, cfg) =>
    {
        cfg.Host(region, h =>
        {
            h.AccessKey(accessKey);
            h.SecretKey(secretKey);
            h.Config(new AmazonSQSConfig { ServiceURL = serviceUrl });
            h.Config(new AmazonSimpleNotificationServiceConfig { ServiceURL = serviceUrl });
        });

        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// POST /transactions
app.MapPost("/transactions", async (
    CreateTransactionRequest request,
    ITransactionService transactionService,
    CancellationToken ct) =>
{
    if (request.Amount <= 0)
        return Results.BadRequest(new { error = "El monto debe ser mayor que cero." });

    if (request.AccountId == Guid.Empty)
        return Results.BadRequest(new { error = "AccountId inválido." });

    var transaction = await transactionService.CreateAsync(request, ct);

    return Results.Created($"/transactions/{transaction.Id}", new
    {
        transaction.Id,
        transaction.AccountId,
        transaction.Amount,
        transaction.Type,
        transaction.Status,
        transaction.CreatedAt
    });
})
.WithName("CreateTransaction")
.WithOpenApi();

app.Run();
