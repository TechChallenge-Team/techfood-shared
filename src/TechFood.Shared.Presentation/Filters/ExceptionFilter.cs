using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TechFood.Shared.Presentation.Filters;

public class ExceptionFilter : IExceptionFilter
{
    private static readonly Type[] _handledExceptions =
    [
        typeof(Domain.Exceptions.DomainException),
        typeof(Application.Exceptions.ApplicationException)
    ];

    public void OnException(ExceptionContext context)
    {
        context.ExceptionHandled = true;

        var requestId = context.HttpContext.TraceIdentifier;

        if (_handledExceptions.Any(type => type.IsInstanceOfType(context.Exception)))
        {
            context.Result = new BadRequestObjectResult(
                new
                {
                    requestId,
                    message = context.Exception.Message
                });
        }
        else
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ExceptionFilter>>();

            logger.LogError(
                context.Exception,
                "One or more error has occurried. RequestId {requestId}",
                requestId);

            var result = new ObjectResult(
                new
                {
                    requestId,
                    message = context.Exception.ToString(),
                })
            {
                StatusCode = 500
            };

            context.Result = result;
        }
    }
}
