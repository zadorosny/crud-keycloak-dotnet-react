using MfaCrud.Api.Auth;
using Microsoft.Extensions.Options;

namespace MfaCrud.Api.Keycloak;

public static class KeycloakServiceCollectionExtensions
{
    public static IServiceCollection AddKeycloakAdminApi(this IServiceCollection services)
    {
        services.AddSingleton<ServiceTokenCache>();
        services.AddExceptionHandler<KeycloakAdminExceptionHandler>();

        services.AddHttpClient<KeycloakAdminClient>((provider, http) =>
        {
            var options = provider.GetRequiredService<IOptions<KeycloakOptions>>().Value;
            http.BaseAddress = new Uri($"{options.BaseUrl.TrimEnd('/')}/");
            http.Timeout = TimeSpan.FromSeconds(15);
        });

        return services;
    }
}
