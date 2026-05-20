using Grand.SharedKernel.IntegrationEvents;

namespace Grand.Infrastructure.Messaging;

public interface IEventBus
{
    Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
        where T : IntegrationEvent;
}
