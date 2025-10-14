using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TechFood.Shared.Presentation.Filters;

public class ModelStateFilter : ActionFilterAttribute
{
  public override void OnActionExecuting(ActionExecutingContext context)
  {
    if (!context.ModelState.IsValid)
    {
      var requestId = context.HttpContext.TraceIdentifier;

      var errors = context.ModelState
          .Where(e => e.Value!.Errors.Count > 0)
          .ToDictionary(
              kv => kv.Key,
              kv => kv.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
          );

      context.Result = new BadRequestObjectResult(new
      {
        requestId,
        message = "The request is invalid.",
        errors
      });
    }
  }
}
