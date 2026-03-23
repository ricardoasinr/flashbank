using Amazon.SimpleNotificationService;
using Amazon.SQS;
using FlashBank.History.Consumers;
using FlashBank.Shared.Events;
using MassTransit;

var builder = Host.CreateApplicationBuilder(args);

var awsSection = builder.Configuration.GetSection("AWS");
var serviceUrl  = awsSection["ServiceURL"] ?? "http://localhost:4566";
var accessKey   = awsSection["AccessKey"]  ?? "test";
var secretKey   = awsSection["SecretKey"]  ?? "test";
var region      = awsSection["Region"]     ?? "us-east-1";

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<HistoryConsumer>();

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

        cfg.ConfigureEndpoints(ctx);
    });
});

var host = builder.Build();
host.Run();
