using Microsoft.Extensions.Logging;
using Swap.Htmx.Attributes;
using Swap.Htmx.Events;
using Swap.Htmx.Models;
using SwapDebtors.Events;

namespace SwapDebtors.Handlers;

[SwapHandler(Priority = 100)]
public sealed class DebtorCreatedAuditHandler : ISwapEventHandler<DebtorCreatedEvent>
{
    private readonly ILogger<DebtorCreatedAuditHandler> _logger;

    public DebtorCreatedAuditHandler(ILogger<DebtorCreatedAuditHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(DebtorCreatedEvent @event, SwapResponseBuilder builder, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Handler] Debtor created: {Name} (Id: {Id})", @event.Name, @event.Id);
        return Task.CompletedTask;
    }
}
