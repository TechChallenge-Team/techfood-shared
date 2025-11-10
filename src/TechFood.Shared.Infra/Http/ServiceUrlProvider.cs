using System;
using Microsoft.Extensions.Configuration;

namespace TechFood.Shared.Infra.Http;

public class ServiceUrlProvider : IServiceUrlProvider
{
    private readonly IConfiguration _configuration;

    public ServiceUrlProvider(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public string GetServiceUrl(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            throw new ArgumentException("Service name cannot be null or empty.", nameof(serviceName));
        }

        var baseUrl = _configuration[$"Services:{serviceName}"];

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException($"Base URL for service '{serviceName}' is not configured in 'Services:{serviceName}'.");
        }

        return NormalizeUrl(baseUrl);
    }

    public Uri GetServiceUri(string serviceName)
    {
        var url = GetServiceUrl(serviceName);
        return new Uri(url);
    }

    private static string NormalizeUrl(string url)
    {
        var trimmedUrl = url.TrimEnd('/');
        return trimmedUrl + '/';
    }
}
