using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TechFood.Shared.Application.Events;
using TechFood.Shared.Infra.Extensions;

namespace TechFood.Shared.Worker.Events
{
    internal class EventBusWorker : BackgroundService
    {
        private readonly InfraOptions _infraOptions;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EventBusWorker> _logger;

        public EventBusWorker(
            IServiceProvider serviceProvider,
            IOptions<InfraOptions> infraOptions,
            ILogger<EventBusWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _infraOptions = infraOptions.Value;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var handlers = _infraOptions.ApplicationAssembly?.GetTypes()
                    .Where(t => t.GetInterfaces().Any(i =>
                                i.IsGenericType &&
                                i.GetGenericTypeDefinition() == typeof(INotificationHandler<>) &&
                                i.GetGenericArguments()[0].GetInterfaces().Contains(typeof(IIntegrationEvent))
                                )) ?? [];

            foreach (var handlerType in handlers)
            {
                var eventType = handlerType.GetInterfaces()
                    .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
                    .GetGenericArguments()[0];

                var method = typeof(IEventBus).GetMethod("Subscribe")!
                    .MakeGenericMethod(eventType, handlerType);

                _logger.LogInformation("Subscribing integration event {EventType} with handler {HandlerType}", eventType.Name, handlerType.Name);

                var eventBus = _serviceProvider.GetRequiredService<IEventBus>();

                method.Invoke(eventBus, null);
            }

            return Task.CompletedTask;
        }
    }
}
