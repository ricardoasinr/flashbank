using Amazon.SimpleNotificationService;
using Amazon.SQS;
using FlashBank.Transactions.Consumers;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var awsSection = builder.Configuration.GetSection("AWS");
var serviceUrl  = awsSection["ServiceURL"] ?? "http://localhost:4566";
var accessKey   = awsSection["AccessKey"]  ?? "test";
var secretKey   = awsSection["SecretKey"]  ?? "test";
var region      = awsSection["Region"]     ?? "us-east-1";

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

app.Run();
