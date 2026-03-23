using FlashBank.Shared.Events;
using MassTransit;

namespace FlashBank.History.Consumers;

/// <summary>
/// El historial se registra desde FlashBank.Transactions (TransactionService.UpdateStatusAsync)
/// una vez que el status es definitivo (Completed o Failed).
/// Este consumer ya no escribe en MongoDB para evitar registros duplicados con Status=Pending.
/// </summary>
public class HistoryConsumer : IConsumer<TransactionCreated>
{
    private readonly ILogger<HistoryConsumer> _logger;

    public HistoryConsumer(ILogger<HistoryConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<TransactionCreated> context)
    {
        _logger.LogDebug(
            "[HistoryConsumer] TransactionCreated recibido (sin acción — historial gestionado por TransactionService) → TransactionId: {TransactionId}",
            context.Message.TransactionId);

        return Task.CompletedTask;
    }
}
