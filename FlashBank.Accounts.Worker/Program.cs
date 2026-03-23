using Amazon.SimpleNotificationService;
using Amazon.SQS;
using FlashBank.Accounts.Worker.Consumers;
using FlashBank.Accounts.Worker.Services;
using MassTransit;

var builder = Host.CreateApplicationBuilder(args);

var awsSection = builder.Configuration.GetSection("AWS");
var serviceUrl  = awsSection["ServiceURL"] ?? "http://localhost:4566";
var accessKey   = awsSection["AccessKey"]  ?? "test";
var secretKey   = awsSection["SecretKey"]  ?? "test";
var region      = awsSection["Region"]     ?? "us-east-1";

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

        cfg.ConfigureEndpoints(ctx);
    });
});

var host = builder.Build();
host.Run();
