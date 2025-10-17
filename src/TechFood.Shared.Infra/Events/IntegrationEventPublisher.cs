using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TechFood.Shared.Application.Events;

namespace TechFood.Shared.Infra.Events;

/// <summary>
/// Default implementation of IIntegrationEventPublisher.
/// This is a placeholder that logs events. Replace with actual broker implementation (RabbitMQ, Azure Service Bus, etc.).
/// </summary>
public class IntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly ILogger<IntegrationEventPublisher> _logger;

    public IntegrationEventPublisher(ILogger<IntegrationEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        // TODO: Replace this with actual message broker implementation
        // Examples:
        // - RabbitMQ: using MassTransit or RabbitMQ.Client
        // - Azure Service Bus: using Azure.Messaging.ServiceBus
        // - Amazon SQS: using AWSSDK.SQS
        // - Kafka: using Confluent.Kafka

        _logger.LogInformation(
            "Integration event {EventType} published. Implement IIntegrationEventPublisher to send to your message broker.",
            integrationEvent.GetType().Name);

        return Task.CompletedTask;
    }
}
