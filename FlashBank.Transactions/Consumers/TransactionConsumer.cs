using FlashBank.Shared.Events;
using MassTransit;

namespace FlashBank.Transactions.Consumers;

public class TransactionConsumer : IConsumer<TransactionUpdate>
{
    private readonly ILogger<TransactionConsumer> _logger;

    public TransactionConsumer(ILogger<TransactionConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<TransactionUpdate> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "[TransactionConsumer] TransactionUpdate recibido → TransactionId: {TransactionId} | NewStatus: {NewStatus} | Message: {Message}",
            msg.TransactionId, msg.NewStatus, msg.Message ?? "N/A");

        // TODO: implementar UpdateStatus con EF Core + PostgreSQL (flashbank_transactions)
        // Cambiar registro de Status="Pending" a Status=msg.NewStatus

        return Task.CompletedTask;
    }
}
