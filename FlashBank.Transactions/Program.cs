using Amazon.SimpleNotificationService;
using Amazon.SQS;
using FlashBank.Shared.Enums;
using FlashBank.Shared.Events;
using FlashBank.Transactions.Consumers;
using FlashBank.Transactions.Data;
using FlashBank.Transactions.DTOs;
using FlashBank.Transactions.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core — PostgreSQL (flashbank_transactions)
builder.Services.AddDbContext<TransactionDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TransactionsDb")));

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
// Acción 1: Inserta en BD con Status=Pending
// Acción 2: Publica TransactionCreated hacia el primer SQS
app.MapPost("/transactions", async (
    CreateTransactionRequest request,
    TransactionDbContext db,
    IBus bus,
    CancellationToken ct) =>
{
    if (request.Amount <= 0)
        return Results.BadRequest(new { error = "El monto debe ser mayor que cero." });

    if (request.AccountId == Guid.Empty)
        return Results.BadRequest(new { error = "AccountId inválido." });

    var transaction = new Transaction
    {
        Id        = Guid.NewGuid(),
        AccountId = request.AccountId,
        Amount    = request.Amount,
        Type      = request.Type,
        Status    = TransactionStatus.Pending,
        CreatedAt = DateTime.UtcNow
    };

    db.Transactions.Add(transaction);
    await db.SaveChangesAsync(ct);

    var evt = new TransactionCreated(
        transaction.Id,
        transaction.AccountId,
        transaction.Amount,
        transaction.Type,
        transaction.CreatedAt);

    await bus.Publish(evt, ct);

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
