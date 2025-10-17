using System.Threading;
using System.Threading.Tasks;

namespace TechFood.Shared.Application.Events;

/// <summary>
/// Interface for publishing integration events to a message broker.
/// </summary>
public interface IIntegrationEventPublisher
{
    /// <summary>
    /// Publishes an integration event to the message broker.
    /// </summary>
    /// <param name="integrationEvent">The integration event to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
