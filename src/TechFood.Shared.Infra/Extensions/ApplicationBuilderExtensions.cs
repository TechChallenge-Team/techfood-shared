using Microsoft.Extensions.DependencyInjection;
using TechFood.Shared.Infra.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.AspNetCore.Builder
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseSharedInfra(this IApplicationBuilder app)
        {
            app.UseMiddleware<TechFood.Shared.Infra.EventualConsistency.Middleware>();

            return app;
        }

        public static IApplicationBuilder RunMigration<DbContext>(this IApplicationBuilder app) where DbContext : TechFoodContext
        {
            using var scope = app.ApplicationServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DbContext>();
            context.Database.Migrate();
            return app;
        }
    }
}
