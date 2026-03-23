using Amazon.SimpleNotificationService;
using Amazon.SQS;
using FlashBank.Shared.Events;
using FlashBank.Transactions.Consumers;
using FlashBank.Transactions.Data;
using FlashBank.Transactions.Extensions;
using FlashBank.Transactions.Repositories;
using FlashBank.Transactions.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<TransactionDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TransactionsDb")));

var mongoSection      = builder.Configuration.GetSection("MongoDB");
var mongoConnectionStr = mongoSection["ConnectionString"] ?? throw new InvalidOperationException("MongoDB:ConnectionString no configurado.");
var mongoDatabaseName = mongoSection["Database"] ?? "mongodb-history";

var mongoClient   = new MongoClient(mongoConnectionStr);
var mongoDatabase = mongoClient.GetDatabase(mongoDatabaseName);

builder.Services.AddSingleton<IMongoClient>(mongoClient);
builder.Services.AddSingleton(mongoDatabase);
builder.Services.AddSingleton<IHistoryRepository, HistoryRepository>();

builder.Services.AddScoped<ITransactionService, TransactionService>();

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

        cfg.Message<TransactionCreated>(x => x.SetEntityName("transaction-created"));
        cfg.Message<TransactionUpdate>(x => x.SetEntityName("transaction-update"));

        cfg.ReceiveEndpoint("transaction-update-queue", e =>
        {
            e.ConfigureConsumer<TransactionConsumer>(ctx);
        });
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapTransactionEndpoints();
app.Run();
