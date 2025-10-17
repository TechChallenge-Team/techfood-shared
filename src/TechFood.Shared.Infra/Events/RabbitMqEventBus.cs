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

        _channel.BasicPublish(
            exchange: ExchangeName,
            routingKey: routingKey,
            basicProperties: null,
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
        consumer.Received += async (model, @event) =>
        {
            var body = @event.Body.ToArray();
            var message = JsonSerializer.Deserialize<T>(body);

            using var scope = _serviceProvider.CreateScope();

            await _mediator.Publish(message!);

            _logger.LogInformation("Processed integration event from RabbitMQ: {EventType}", typeof(T).Name);
        };

        _channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
