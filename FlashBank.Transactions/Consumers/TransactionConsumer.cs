using FlashBank.Shared.Events;
using FlashBank.Transactions.Services;
using MassTransit;

namespace FlashBank.Transactions.Consumers;

public class TransactionConsumer : IConsumer<TransactionUpdate>
{
    private readonly ITransactionService _transactionService;
    private readonly ILogger<TransactionConsumer> _logger;

    public TransactionConsumer(ITransactionService transactionService, ILogger<TransactionConsumer> logger)
    {
        _transactionService = transactionService;
        _logger             = logger;
    }

    public async Task Consume(ConsumeContext<TransactionUpdate> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "[TransactionConsumer] TransactionUpdate recibido → TransactionId: {TransactionId} | NewStatus: {NewStatus}",
            msg.TransactionId, msg.NewStatus);

        await _transactionService.UpdateStatusAsync(
            msg.TransactionId,
            msg.NewStatus,
            context.CancellationToken);
    }
}
