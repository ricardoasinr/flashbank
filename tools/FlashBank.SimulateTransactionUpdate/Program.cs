using System.Linq;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using FlashBank.Shared.Enums;
using FlashBank.Shared.Events;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Simula el segundo paso del flujo EDA: publicar TransactionUpdate como lo hace AccountConsumer.
// Uso:
//   dotnet run --project tools/FlashBank.SimulateTransactionUpdate -- <TransactionId> <AccountId> <Completed|Failed> [mensajeError]
//
// Ejemplo:
//   dotnet run --project tools/FlashBank.SimulateTransactionUpdate -- \
//     "3fa85f64-5717-4562-b3fc-2c963f66afa6" "a1b2c3d4-e5f6-7890-abcd-ef1234567890" Completed

if (args.Length < 3)
{
    Console.Error.WriteLine(
        "Uso: dotnet run -- <TransactionId> <AccountId> <Completed|Failed> [mensajeErrorOpcional]");
    return 1;
}

if (!Guid.TryParse(args[0], out var transactionId) || !Guid.TryParse(args[1], out var accountId))
{
    Console.Error.WriteLine("TransactionId y AccountId deben ser GUID válidos.");
    return 1;
}

if (!Enum.TryParse<TransactionStatus>(args[2], ignoreCase: true, out var status) ||
    status is not (TransactionStatus.Completed or TransactionStatus.Failed))
{
    Console.Error.WriteLine("El tercer argumento debe ser Completed o Failed.");
    return 1;
}

var errorMessage = args.Length > 3 ? string.Join(" ", args.Skip(3)) : null;
if (status == TransactionStatus.Failed && string.IsNullOrWhiteSpace(errorMessage))
    errorMessage = "Simulación manual desde FlashBank.SimulateTransactionUpdate";

var builder = Host.CreateApplicationBuilder(args);

var aws = builder.Configuration.GetSection("AWS");
var serviceUrl = aws["ServiceURL"] ?? "http://localhost:4566";
var accessKey  = aws["AccessKey"]  ?? "test";
var secretKey  = aws["SecretKey"]  ?? "test";
var region     = aws["Region"]     ?? "us-east-1";

builder.Services.AddMassTransit(x =>
{
    x.UsingAmazonSqs((_, cfg) =>
    {
        cfg.Host(region, h =>
        {
            h.AccessKey(accessKey);
            h.SecretKey(secretKey);
            h.Config(new AmazonSQSConfig { ServiceURL = serviceUrl });
            h.Config(new AmazonSimpleNotificationServiceConfig { ServiceURL = serviceUrl });
        });

        cfg.Message<TransactionUpdate>(x => x.SetEntityName("transaction-update"));
    });
});

var host = builder.Build();
await host.StartAsync();

try
{
    var bus = host.Services.GetRequiredService<IBus>();
    var update = new TransactionUpdate(transactionId, accountId, status, errorMessage);
    await bus.Publish(update);
    Console.WriteLine($"Publicado TransactionUpdate → Tx={transactionId} Status={status}");
}
finally
{
    await host.StopAsync();
}

return 0;
