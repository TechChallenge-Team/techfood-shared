using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TechFood.Shared.Application.Events;

namespace TechFood.Shared.Infra.Events;

public class RabbitMqEventBus : IEventBus, IDisposable
{
    private readonly ILogger<RabbitMqEventBus> _logger;

    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMediator _mediator;

    private const string ExchangeName = "app_exchange";

    public RabbitMqEventBus(
        ILogger<RabbitMqEventBus> logger,
        IServiceProvider serviceProvider,
        [FromKeyedServices(EventualConsistency.Mediator.ServiceKey)] IMediator mediator)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _mediator = mediator;

        var factory = new ConnectionFactory() { HostName = "localhost" };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Topic, durable: true);
    }

    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        var routingKey = typeof(T).Name;

        var body = JsonSerializer.SerializeToUtf8Bytes(@event);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true; // Make messages persistent

        _channel.BasicPublish(
            exchange: ExchangeName,
            routingKey: routingKey,
            basicProperties: properties,
            body: body);

        _logger.LogInformation("Published integration event to RabbitMQ: {EventType}", routingKey);

        return Task.CompletedTask;
    }

    public void Subscribe<T, TH>()
         where T : IIntegrationEvent
         where TH : INotificationHandler<T>
    {
        var queueName = $"{typeof(T).Name}_queue";

        _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queue: queueName, exchange: ExchangeName, routingKey: typeof(T).Name);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            
            try
            {
                var message = JsonSerializer.Deserialize<T>(body);

                // Create a new scope for each message to ensure proper DI lifetime management
                using var scope = _serviceProvider.CreateScope();
                var scopedMediator = scope.ServiceProvider.GetRequiredKeyedService<IMediator>(EventualConsistency.Mediator.ServiceKey);
                
                // Process the message
                await scopedMediator.Publish(message!, CancellationToken.None);

                // Manually acknowledge the message only after successful processing
                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                
                _logger.LogInformation("Processed integration event from RabbitMQ: {EventType}", typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing integration event {EventType}. Message will be requeued.", typeof(T).Name);
                
                // Reject and requeue the message for retry
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        // Set autoAck to false to manually control acknowledgment
        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        
        _logger.LogInformation("Subscribed to RabbitMQ queue {QueueName} for event {EventType}", queueName, typeof(T).Name);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }

    public Task PublishAsync<T>(T eventMessage, string? callbackName = null, CancellationToken cancellationToken = default) where T : IIntegrationEvent
    {
        throw new NotImplementedException();
    }
}
