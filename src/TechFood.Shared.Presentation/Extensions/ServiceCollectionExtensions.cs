using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TechFood.Shared.Presentation.Extensions;
using TechFood.Shared.Presentation.Filters;
using TechFood.Shared.Presentation.NamingPolicy;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedPresentation(this IServiceCollection services, IConfiguration configuration, PresentationOptions? options = null)
    {
        options ??= new PresentationOptions();

        services
            .AddControllers(mvcOptions =>
            {
                mvcOptions.Filters.Add<ExceptionFilter>();
                mvcOptions.Filters.Add<ModelStateFilter>();
            })
            .ConfigureApiBehaviorOptions(apiOptions =>
            {
                apiOptions.SuppressModelStateInvalidFilter = true;
            })
            .AddJsonOptions(jsonOptions =>
            {
                jsonOptions.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                jsonOptions.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(new UpperCaseNamingPolicy()));
                //jsonOptions.JsonSerializerOptions.Converters.Add(new JsonTimeSpanConverter());
            });

        services.AddCors();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.All;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        services.AddHttpContextAccessor();

        if (options.AddSwagger)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = options.SwaggerTitle ?? "API",
                    Version = "v1",
                    Description = options.SwaggerDescription ?? "API Documentation",
                });
            });
        }

        if (options.AddHealthChecks)
        {
            services.AddHealthChecks();
        }

        if (options.AddJwtAuthentication)
        {
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(o =>
                {
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = configuration["Jwt:Issuer"],
                        ValidAudience = configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)),
                    };
                });
        }

        return services;
    }
}
