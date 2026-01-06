using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TechFood.Shared.Domain.UoW;

namespace TechFood.Shared.Infra.Persistence.Behaviors;

/// <summary>
/// Generic notification handler that automatically commits database changes after all specific handlers execute.
/// This handler is registered LAST in DI container, ensuring it runs after all domain-specific notification handlers.
/// 
/// IMPORTANT: This handler only works in Worker contexts (no HTTP request).
/// In API contexts, the EventualConsistency.Middleware is responsible for transaction management.
/// </summary>
/// <typeparam name="TNotification">Any notification type (domain events, integration events)</typeparam>
public class SaveChangesNotificationHandler<TNotification> : INotificationHandler<TNotification>
    where TNotification : INotification
{
    private readonly IUnitOfWorkTransaction _transaction;
    private readonly ILogger<SaveChangesNotificationHandler<TNotification>> _logger;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public SaveChangesNotificationHandler(
        IUnitOfWorkTransaction transaction,
        ILogger<SaveChangesNotificationHandler<TNotification>> logger,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _transaction = transaction;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task Handle(TNotification notification, CancellationToken cancellationToken)
    {
        // Skip commit in HTTP context - the Middleware handles transaction management
        if (_httpContextAccessor?.HttpContext is not null)
            return;

        try
        {
            var committed = await _transaction.CommitAsync();
            
            if (committed)
            {
                _logger.LogInformation(
                    "SaveChangesNotificationHandler: Changes committed to database after processing {NotificationType}",
                    typeof(TNotification).Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "SaveChangesNotificationHandler: Failed to commit changes after processing {NotificationType}",
                typeof(TNotification).Name);
            throw;
        }
    }
}
