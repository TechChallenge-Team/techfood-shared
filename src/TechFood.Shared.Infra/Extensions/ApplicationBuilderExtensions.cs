namespace Microsoft.AspNetCore.Builder
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseSharedInfra(this IApplicationBuilder app)
        {
            app.UseMiddleware<TechFood.Shared.Infra.EventualConsistency.Middleware>();

            return app;
        }
    }
}
