using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Configuration;
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

    private const string ExchangeName = "techfood.events.exchange";
    private const string DeadLetterExchangeName = "techfood.events.dlx";
    private const string RetryExchangeName = "techfood.events.retry";
    private const int MaxRetryAttempts = 3;
    private static readonly int[] RetryDelaysMs = { 1000, 5000, 15000 }; // 1s, 5s, 15s

    public RabbitMqEventBus(
        ILogger<RabbitMqEventBus> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        // get RabbitMQ connection settings from configuration if needed

        var factory = new ConnectionFactory()
        {
            HostName = configuration.GetValue<string>("EventBus:RabbitMQ:HostName") ?? "localhost",
            UserName = configuration.GetValue<string>("EventBus:RabbitMQ:UserName") ?? "guest",
            Password = configuration.GetValue<string>("EventBus:RabbitMQ:Password") ?? "guest",
            Port = configuration.GetValue<int>("EventBus:RabbitMQ:Port"),
            DispatchConsumersAsync = true // Enable async consumers
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Topic, durable: true);
        
        // Declare Dead Letter Exchange for failed messages
        _channel.ExchangeDeclare(exchange: DeadLetterExchangeName, type: ExchangeType.Topic, durable: true);
        
        // Declare Retry Exchange for delayed retries
        _channel.ExchangeDeclare(exchange: RetryExchangeName, type: ExchangeType.Topic, durable: true);
    }

    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        var routingKey = @event.GetType().Name;

        // Serialize using the concrete type to include all derived properties
        var body = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType());

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
        var serviceName = Assembly.GetEntryAssembly()!.GetName().Name;
        var queueName = $"{serviceName}_{typeof(T).Name}_queue";
        var dlqName = $"{queueName}.dead-letter";

        // Declare Dead Letter Queue
        _channel.QueueDeclare(queue: dlqName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queue: dlqName, exchange: DeadLetterExchangeName, routingKey: typeof(T).Name);

        // Declare retry queues with TTL (one per retry level)
        for (int i = 0; i < RetryDelaysMs.Length; i++)
        {
            var retryQueueName = $"{queueName}.retry.{i + 1}";
            var retryQueueArgs = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", ExchangeName },
                { "x-dead-letter-routing-key", typeof(T).Name },
                { "x-message-ttl", RetryDelaysMs[i] }
            };
            _channel.QueueDeclare(queue: retryQueueName, durable: true, exclusive: false, autoDelete: false, arguments: retryQueueArgs);
            _channel.QueueBind(queue: retryQueueName, exchange: RetryExchangeName, routingKey: $"{typeof(T).Name}.retry.{i + 1}");
        }

        // Declare main queue with DLX configuration
        var queueArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", DeadLetterExchangeName },
            { "x-dead-letter-routing-key", typeof(T).Name }
        };
        
        _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
        _channel.QueueBind(queue: queueName, exchange: ExchangeName, routingKey: typeof(T).Name);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var retryCount = GetRetryCount(ea.BasicProperties);

            try
            {
                var message = JsonSerializer.Deserialize<T>(body);

                // Create a new scope for each message to ensure proper DI lifetime management
                using var scope = _serviceProvider.CreateScope();
                var scopedMediator = scope.ServiceProvider.GetRequiredKeyedService<IMediator>(EventualConsistency.Mediator.ServiceKey);

                // Process the message - SaveChangesNotificationHandler will commit automatically
                await scopedMediator.Publish(message!, CancellationToken.None);

                // Manually acknowledge the message only after successful processing
                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);

                _logger.LogInformation("Processed integration event from RabbitMQ: {EventType}", typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error processing integration event {EventType}. Retry attempt {RetryCount} of {MaxRetries}", 
                    typeof(T).Name, retryCount + 1, MaxRetryAttempts);

                // Acknowledge original message first
                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);

                if (retryCount < MaxRetryAttempts)
                {
                    // Send to retry queue with delay
                    var newProperties = _channel.CreateBasicProperties();
                    newProperties.Persistent = true;
                    newProperties.Headers = new Dictionary<string, object>
                    {
                        ["x-retry-count"] = retryCount + 1
                    };

                    // Route to appropriate retry queue based on retry count
                    var retryRoutingKey = $"{typeof(T).Name}.retry.{retryCount + 1}";
                    
                    _channel.BasicPublish(
                        exchange: RetryExchangeName,
                        routingKey: retryRoutingKey,
                        basicProperties: newProperties,
                        body: body);

                    _logger.LogWarning(
                        "Message sent to retry queue with {DelayMs}ms delay. Retry {RetryCount}/{MaxRetries}: {EventType}", 
                        RetryDelaysMs[retryCount], retryCount + 1, MaxRetryAttempts, typeof(T).Name);
                }
                else
                {
                    // Max retries exceeded - send to DLQ
                    _logger.LogError(
                        "Max retry attempts ({MaxRetries}) exceeded for {EventType}. Moving to Dead Letter Queue.", 
                        MaxRetryAttempts, typeof(T).Name);

                    var dlqProperties = _channel.CreateBasicProperties();
                    dlqProperties.Persistent = true;
                    dlqProperties.Headers = new Dictionary<string, object>
                    {
                        ["x-retry-count"] = retryCount,
                        ["x-final-error"] = ex.Message
                    };

                    _channel.BasicPublish(
                        exchange: DeadLetterExchangeName,
                        routingKey: typeof(T).Name,
                        basicProperties: dlqProperties,
                        body: body);
                }
            }
        };

        // Set autoAck to false to manually control acknowledgment
        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

        _logger.LogInformation("Subscribed to RabbitMQ queue {QueueName} for event {EventType} with DLQ {DlqName}", 
            queueName, typeof(T).Name, dlqName);
    }

    private static int GetRetryCount(IBasicProperties properties)
    {
        if (properties?.Headers != null && 
            properties.Headers.TryGetValue("x-retry-count", out var retryCountObj))
        {
            return retryCountObj switch
            {
                int count => count,
                byte[] bytes => BitConverter.ToInt32(bytes, 0),
                _ => 0
            };
        }
        return 0;
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
