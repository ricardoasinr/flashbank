using Amazon.SimpleNotificationService;
using Amazon.SQS;
using FlashBank.Accounts.Worker.Consumers;
using FlashBank.Accounts.Worker.Data;
using FlashBank.Accounts.Worker.Services;
using FlashBank.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

var awsSection = builder.Configuration.GetSection("AWS");
var serviceUrl  = awsSection["ServiceURL"] ?? "http://localhost:4566";
var accessKey   = awsSection["AccessKey"]  ?? "test";
var secretKey   = awsSection["SecretKey"]  ?? "test";
var region      = awsSection["Region"]     ?? "us-east-1";

builder.Services.AddDbContext<AccountDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AccountsDb")));

builder.Services.AddScoped<IAccountService, AccountService>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<AccountConsumer>();

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

        cfg.ConfigureEndpoints(ctx);
    });
});

var host = builder.Build();
host.Run();
