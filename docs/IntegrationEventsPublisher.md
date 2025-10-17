# Integration Events Publisher

## Overview

This infrastructure separates **Domain Events** (processed internally) from **Integration Events** (published to external message brokers).

- **IDomainEvent**: Processed internally using MediatR handlers
- **IIntegrationEvent**: Published to a message broker for inter-service communication

## Default Behavior

By default, `DefaultIntegrationEventPublisher` is registered, which only logs the events. **You must implement your own publisher** to send events to your message broker.

## Implementing Custom Publisher

### 1. Create Your Implementation

Create a class that implements `IIntegrationEventPublisher`:

```csharp
public class RabbitMqIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint; // MassTransit example

    public RabbitMqIntegrationEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        await _publishEndpoint.Publish(integrationEvent, cancellationToken);
    }
}
```

### 2. Register Your Implementation

In your application's `Program.cs` or `Startup.cs`, **after** calling `AddSharedInfra`, replace the default implementation:

```csharp
services.AddSharedInfra<YourDbContext>();

// Replace default integration event publisher
services.AddScoped<IIntegrationEventPublisher, RabbitMqIntegrationEventPublisher>();
```

## Example Implementations

### RabbitMQ with MassTransit

```csharp
// Install: MassTransit.RabbitMQ
public class RabbitMqIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public RabbitMqIntegrationEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        return _publishEndpoint.Publish(integrationEvent, cancellationToken);
    }
}

// Registration in Program.cs
services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq://localhost");
    });
});

services.AddScoped<IIntegrationEventPublisher, RabbitMqIntegrationEventPublisher>();
```

### Azure Service Bus

```csharp
// Install: Azure.Messaging.ServiceBus
public class AzureServiceBusIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly ServiceBusSender _sender;

    public AzureServiceBusIntegrationEventPublisher(ServiceBusClient serviceBusClient)
    {
        _sender = serviceBusClient.CreateSender("integration-events");
    }

    public async Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType());
        var message = new ServiceBusMessage(json)
        {
            Subject = integrationEvent.GetType().Name
        };

        await _sender.SendMessageAsync(message, cancellationToken);
    }
}

// Registration in Program.cs
services.AddSingleton(new ServiceBusClient(connectionString));
services.AddScoped<IIntegrationEventPublisher, AzureServiceBusIntegrationEventPublisher>();
```

### AWS SQS

```csharp
// Install: AWSSDK.SQS
public class AwsSqsIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IAmazonSQS _sqsClient;
    private readonly string _queueUrl;

    public AwsSqsIntegrationEventPublisher(IAmazonSQS sqsClient, IConfiguration configuration)
    {
        _sqsClient = sqsClient;
        _queueUrl = configuration["AWS:SQS:IntegrationEventsQueueUrl"];
    }

    public async Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType());
        var request = new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = json,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["EventType"] = new MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = integrationEvent.GetType().Name
                }
            }
        };

        await _sqsClient.SendMessageAsync(request, cancellationToken);
    }
}

// Registration in Program.cs
services.AddAWSService<IAmazonSQS>();
services.AddScoped<IIntegrationEventPublisher, AwsSqsIntegrationEventPublisher>();
```

### Apache Kafka

```csharp
// Install: Confluent.Kafka
public class KafkaIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IProducer<string, string> _producer;

    public KafkaIntegrationEventPublisher(IConfiguration configuration)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"]
        };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var eventType = integrationEvent.GetType().Name;
        var json = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType());

        await _producer.ProduceAsync(
            "integration-events",
            new Message<string, string>
            {
                Key = eventType,
                Value = json
            },
            cancellationToken);
    }
}

// Registration in Program.cs
services.AddSingleton<IIntegrationEventPublisher, KafkaIntegrationEventPublisher>();
```

## How It Works

1. When you call `mediator.Publish(integrationEvent)`:
   - The custom `Mediator` checks if the event implements `IIntegrationEvent`
   - If yes, it adds to the `IntegrationEventsQueue` in HttpContext
   - If no (domain event), it adds to the regular `EventsQueue`

2. At the end of the request, the `Middleware`:
   - Processes domain events internally using MediatR handlers
   - Publishes integration events to the message broker via `IIntegrationEventPublisher`
   - Commits the transaction only after all events are processed

## Creating Integration Events

```csharp
public record OrderCreatedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    DateTime CreatedAt
) : IIntegrationEvent;
```

Then publish it:

```csharp
await _mediator.Publish(new OrderCreatedIntegrationEvent(
    order.Id,
    order.CustomerId,
    order.TotalAmount,
    DateTime.UtcNow
));
```

The event will be published to your configured message broker at the end of the request, after transaction commit.
