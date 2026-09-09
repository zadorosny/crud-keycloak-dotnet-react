using MfaCrud.Api.Auth;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace MfaCrud.Api.Keycloak;

/// <summary>
/// A API só consegue validar tokens se alcançar o discovery document do realm (é de lá que vêm as
/// chaves públicas). Sem isso, tudo responde 401 — melhor aparecer no /health.
/// </summary>
public sealed class KeycloakDiscoveryHealthCheck(HttpClient http, IOptions<KeycloakOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var discovery = $"{options.Value.Authority.TrimEnd('/')}/.well-known/openid-configuration";

        try
        {
            using var response = await http.GetAsync(discovery, cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy($"Discovery document answered {(int)response.StatusCode}.");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return HealthCheckResult.Unhealthy("Discovery document is unreachable.", exception);
        }
    }
}
