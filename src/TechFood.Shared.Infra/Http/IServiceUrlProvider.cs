using System;

namespace TechFood.Shared.Infra.Http;

public interface IServiceUrlProvider
{
    string GetServiceUrl(string serviceName);

    Uri GetServiceUri(string serviceName);
}
