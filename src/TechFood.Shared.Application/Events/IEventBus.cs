using System.Threading;
using System.Threading.Tasks;

namespace TechFood.Shared.Application.Events;

public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IIntegrationEvent;

    void Subscribe<T, TH>()
        where T : IIntegrationEvent
        where TH : MediatR.INotificationHandler<T>;
}
