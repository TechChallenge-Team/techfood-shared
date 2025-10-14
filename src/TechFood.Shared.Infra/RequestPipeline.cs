using Microsoft.AspNetCore.Builder;

namespace TechFood.Shared.Infra
{
    public static class RequestPipeline
    {
        public static IApplicationBuilder UseInfra(this IApplicationBuilder app)
        {
            app.UseMiddleware<EventualConsistency.Middleware>();

            return app;
        }
    }
}
