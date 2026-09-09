using System.Net.Http.Headers;
using Testcontainers.Keycloak;
using Testcontainers.PostgreSql;

namespace MfaCrud.Api.Tests.Infrastructure;

/// <summary>
/// Boots the real dependencies once per test run: PostgreSQL and a Keycloak importing the very
/// same keycloak/realm-export.json the compose stack uses.
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    public const string KeycloakImage = "quay.io/keycloak/keycloak:26.7.3";
    public const string PostgresImage = "postgres:18";
    public const string AdminServiceSecret = "test-admin-service-secret";
    public const string TwoFactorUser = "customer2fa@test.local";
    public const string TwoFactorPassword = "Customer123!";

    private static readonly TimeSpan TotpWindow = TimeSpan.FromSeconds(30);

    private readonly SemaphoreSlim _totpGate = new(1, 1);
    private string? _lastUsedTotp;

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder(PostgresImage)
        .WithDatabase("mfacrud")
        .Build();

    private readonly KeycloakContainer _keycloak = new KeycloakBuilder(KeycloakImage)
        .WithResourceMapping(RealmFile(), "/opt/keycloak/data/import/")
        .WithEnvironment("MFACRUD_ADMIN_SVC_SECRET", AdminServiceSecret)
        .WithCommand("--import-realm")
        .Build();

    public MfaCrudApiFactory Factory { get; private set; } = null!;

    public TokenClient Tokens { get; private set; } = null!;

    public string BaseUrl => _keycloak.GetBaseAddress().TrimEnd('/');

    public string Authority => $"{BaseUrl}/realms/mfacrud";

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _keycloak.StartAsync());
        Factory = new MfaCrudApiFactory(_postgres.GetConnectionString(), BaseUrl, AdminServiceSecret);
        Tokens = new TokenClient(Authority);
    }

    /// <summary>An API client carrying a real Keycloak token for the given user.</summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(string username, string password, string? totp = null)
    {
        var token = await Tokens.GetTokenAsync(username, password, totp);
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// A client for the user that has an authenticator configured. The realm forbids reusing an OTP
    /// code, so signing in twice inside the same 30s window means waiting for the next one.
    /// </summary>
    public async Task<HttpClient> CreateTwoFactorClientAsync(
        string username = TwoFactorUser, string password = TwoFactorPassword)
    {
        await _totpGate.WaitAsync();
        try
        {
            if (TokenClient.CurrentTotp() == _lastUsedTotp)
            {
                await WaitForNextTotpWindowAsync();
            }

            var totp = TokenClient.CurrentTotp();
            _lastUsedTotp = totp;
            return await CreateAuthenticatedClientAsync(username, password, totp);
        }
        finally
        {
            _totpGate.Release();
        }
    }

    private static async Task WaitForNextTotpWindowAsync()
    {
        var millisecondsIntoWindow = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % TotpWindow.TotalMilliseconds;
        await Task.Delay(TotpWindow - TimeSpan.FromMilliseconds(millisecondsIntoWindow) + TimeSpan.FromMilliseconds(500));
    }

    public async Task DisposeAsync()
    {
        Tokens?.Dispose();
        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }

        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _keycloak.DisposeAsync().AsTask());
    }

    private static FileInfo RealmFile()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "keycloak", "realm-export.json")))
        {
            directory = directory.Parent;
        }

        return directory is null
            ? throw new InvalidOperationException($"keycloak/realm-export.json not found above {AppContext.BaseDirectory}.")
            : new FileInfo(Path.Combine(directory.FullName, "keycloak", "realm-export.json"));
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "api";
}
