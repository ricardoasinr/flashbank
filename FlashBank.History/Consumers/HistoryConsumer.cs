using FlashBank.Shared.Events;
using MassTransit;

namespace FlashBank.History.Consumers;

public class HistoryConsumer : IConsumer<TransactionCreated>
{
    private readonly ILogger<HistoryConsumer> _logger;

    public HistoryConsumer(ILogger<HistoryConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<TransactionCreated> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "[HistoryConsumer] TransactionCreated recibido → TransactionId: {TransactionId} | AccountId: {AccountId} | Amount: {Amount} | Type: {Type} | CreatedAt: {CreatedAt}",
            msg.TransactionId, msg.AccountId, msg.Amount, msg.Type, msg.CreatedAt);

        // TODO: implementar inserción de documento en MongoDB (mongodb-history)
        // Colección: history → documento con todos los campos del evento

        return Task.CompletedTask;
    }
}
