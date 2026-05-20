using Grand.SharedKernel.IntegrationEvents;
using Microsoft.Extensions.Logging;

namespace Grand.Infrastructure.Messaging;

public sealed class InMemoryEventBus(ILogger<InMemoryEventBus> logger) : IEventBus
{
    public Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
        where T : IntegrationEvent
    {
        logger.LogInformation(
            "Integration event published (in-memory): {EventType} {EventId} at {OccurredOn}",
            integrationEvent.EventType,
            integrationEvent.EventId,
            integrationEvent.OccurredOn);

        return Task.CompletedTask;
    }
}
