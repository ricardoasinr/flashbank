using FlashBank.Shared.Events;
using FlashBank.Transactions.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace FlashBank.Transactions.Consumers;

public class TransactionConsumer : IConsumer<TransactionUpdate>
{
    private readonly TransactionDbContext _db;
    private readonly ILogger<TransactionConsumer> _logger;

    public TransactionConsumer(TransactionDbContext db, ILogger<TransactionConsumer> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TransactionUpdate> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "[TransactionConsumer] TransactionUpdate recibido → TransactionId: {TransactionId} | NewStatus: {NewStatus}",
            msg.TransactionId, msg.NewStatus);

        var transaction = await _db.Transactions
            .FirstOrDefaultAsync(t => t.Id == msg.TransactionId, context.CancellationToken);

        if (transaction is null)
        {
            _logger.LogWarning(
                "[TransactionConsumer] Transacción no encontrada → TransactionId: {TransactionId}",
                msg.TransactionId);
            return;
        }

        transaction.Status = msg.NewStatus;
        await _db.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation(
            "[TransactionConsumer] Status actualizado → TransactionId: {TransactionId} | Status: {Status}",
            transaction.Id, transaction.Status);
    }
}
