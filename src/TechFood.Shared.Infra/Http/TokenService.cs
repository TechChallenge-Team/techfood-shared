using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace TechFood.Shared.Infra.Http
{
    internal class TokenService : ITokenService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _config;

        private const string CacheKey = "ServiceAuthToken";
        private const int DefaultTokenExpirationSeconds = 3600;
        private const int TokenRefreshBufferSeconds = 60;

        public TokenService(HttpClient httpClient, IMemoryCache cache, IConfiguration config)
        {
            _httpClient = httpClient;
            _cache = cache;
            _config = config;
        }

        public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
        {
            if (_cache.TryGetValue(CacheKey, out string? token))
            {
                return token!;
            }

            var data = new
            {
                client_id = _config["Authentication:ClientId"],
                client_secret = _config["Authentication:ClientSecret"],
                grant_type = "client_credentials"
            };

            var response = await _httpClient.PostAsJsonAsync("token", data, cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken);
            var expiration = result?.ExpiresIn ?? DefaultTokenExpirationSeconds;

            token = result?.AccessToken ?? throw new Exception("Token inválido da API Auth");

            _cache.Set(
                CacheKey,
                token,
                TimeSpan.FromSeconds(expiration - TokenRefreshBufferSeconds));

            return token;
        }

        private class TokenResponse
        {
            public string AccessToken { get; set; } = null!;

            public int ExpiresIn { get; set; }
        }
    }
}
