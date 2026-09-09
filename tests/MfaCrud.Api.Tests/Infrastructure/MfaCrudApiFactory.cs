using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace MfaCrud.Api.Tests.Infrastructure;

/// <summary>Runs the API against the throwaway containers instead of the local compose stack.</summary>
public sealed class MfaCrudApiFactory(string connectionString, string authority) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        // Added last so it wins over appsettings.Development.json and any local user-secrets.
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = connectionString,
                ["Keycloak:Authority"] = authority,
                ["Keycloak:Audience"] = "mfacrud-api",
            }));
    }
}
