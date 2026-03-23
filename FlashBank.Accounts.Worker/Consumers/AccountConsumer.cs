using FlashBank.Accounts.Worker.Services;
using FlashBank.Shared.Enums;
using FlashBank.Shared.Events;
using MassTransit;

namespace FlashBank.Accounts.Worker.Consumers;

public class AccountConsumer : IConsumer<TransactionCreated>
{
    private readonly IAccountService _accountService;
    private readonly ILogger<AccountConsumer> _logger;

    public AccountConsumer(IAccountService accountService, ILogger<AccountConsumer> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TransactionCreated> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "[AccountConsumer] Mensaje recibido → TransactionId: {TransactionId} | AccountId: {AccountId} | Amount: {Amount} | Type: {Type}",
            msg.TransactionId, msg.AccountId, msg.Amount, msg.Type);

        try
        {
            await _accountService.UpdateBalanceAsync(
                msg.AccountId,
                msg.Amount,
                msg.Type,
                context.CancellationToken);

            var update = new TransactionUpdate(
                msg.TransactionId,
                msg.AccountId,
                TransactionStatus.Completed);

            await context.Publish(update);

            _logger.LogInformation(
                "[AccountConsumer] Balance actualizado. TransactionUpdate(Completed) publicado → TransactionId: {TransactionId}",
                msg.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AccountConsumer] Error al actualizar balance → TransactionId: {TransactionId}",
                msg.TransactionId);

            var update = new TransactionUpdate(
                msg.TransactionId,
                msg.AccountId,
                TransactionStatus.Failed,
                ex.Message);

            await context.Publish(update);
        }
    }
}
