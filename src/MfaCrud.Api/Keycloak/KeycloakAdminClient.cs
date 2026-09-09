using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MfaCrud.Api.Auth;
using Microsoft.Extensions.Options;

namespace MfaCrud.Api.Keycloak;

/// <summary>
/// Talks to the Keycloak Admin REST API as the <c>mfacrud-admin-svc</c> service account. This is
/// the only part of the API that calls Keycloak; everything else works off the access token.
/// </summary>
public sealed class KeycloakAdminClient(
    HttpClient http,
    ServiceTokenCache tokenCache,
    IOptions<KeycloakOptions> options,
    ILogger<KeycloakAdminClient> logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly KeycloakOptions _options = options.Value;

    public async Task<IReadOnlyList<KeycloakUser>> GetUsersAsync(
        int first, int max, string? search, CancellationToken cancellationToken)
    {
        var query = $"users?first={first}&max={max}";
        if (!string.IsNullOrWhiteSpace(search))
        {
            query += $"&search={Uri.EscapeDataString(search)}";
        }

        return await GetAsync<List<KeycloakUser>>(query, cancellationToken) ?? [];
    }

    public async Task<KeycloakUser?> GetUserAsync(Guid id, CancellationToken cancellationToken) =>
        await GetOrNullAsync<KeycloakUser>($"users/{id}", cancellationToken);

    public async Task<IReadOnlyList<KeycloakRole>> GetUserRealmRolesAsync(Guid id, CancellationToken cancellationToken) =>
        await GetAsync<List<KeycloakRole>>($"users/{id}/role-mappings/realm", cancellationToken) ?? [];

    public async Task<KeycloakRole?> GetRealmRoleAsync(string name, CancellationToken cancellationToken) =>
        await GetOrNullAsync<KeycloakRole>($"roles/{Uri.EscapeDataString(name)}", cancellationToken);

    public Task AddRealmRolesAsync(Guid id, IEnumerable<KeycloakRole> roles, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Post, $"users/{id}/role-mappings/realm", roles, cancellationToken);

    public Task RemoveRealmRolesAsync(Guid id, IEnumerable<KeycloakRole> roles, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Delete, $"users/{id}/role-mappings/realm", roles, cancellationToken);

    public async Task<IReadOnlyList<KeycloakCredential>> GetCredentialsAsync(Guid id, CancellationToken cancellationToken) =>
        await GetAsync<List<KeycloakCredential>>($"users/{id}/credentials", cancellationToken) ?? [];

    /// <returns><c>false</c> when Keycloak reports the credential does not exist.</returns>
    public async Task<bool> DeleteCredentialAsync(Guid userId, string credentialId, CancellationToken cancellationToken)
    {
        using var request = await CreateRequestAsync(HttpMethod.Delete, $"users/{userId}/credentials/{credentialId}", cancellationToken);
        using var response = await http.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return true;
    }

    private async Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, path, cancellationToken);
        using var response = await http.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken);
    }

    private async Task<T?> GetOrNullAsync<T>(string path, CancellationToken cancellationToken)
        where T : class
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, path, cancellationToken);
        using var response = await http.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken);
    }

    private async Task SendAsync<TBody>(HttpMethod method, string path, TBody body, CancellationToken cancellationToken)
    {
        using var request = await CreateRequestAsync(method, path, cancellationToken);
        request.Content = JsonContent.Create(body, options: Json);
        using var response = await http.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string path, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, $"admin/realms/{_options.Realm}/{path}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetServiceTokenAsync(cancellationToken));
        return request;
    }

    /// <summary>Client credentials token of the service account, cached until shortly before it expires.</summary>
    private Task<string> GetServiceTokenAsync(CancellationToken cancellationToken) =>
        tokenCache.GetAsync(async ct =>
        {
            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _options.AdminClientId,
                ["client_secret"] = _options.AdminClientSecret,
            };

            using var response = await http.PostAsync(
                $"realms/{_options.Realm}/protocol/openid-connect/token", new FormUrlEncodedContent(form), ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError(
                    "Keycloak refused the service account token request for client {ClientId} with status {Status}.",
                    _options.AdminClientId, (int)response.StatusCode);
                throw new KeycloakAdminException("Could not authenticate against Keycloak.");
            }

            var token = await response.Content.ReadFromJsonAsync<ServiceTokenResponse>(Json, ct)
                ?? throw new KeycloakAdminException("Keycloak returned an empty token response.");

            return (token.AccessToken, TimeSpan.FromSeconds(token.ExpiresIn));
        }, cancellationToken);

    private async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        // Body may carry Keycloak's own error description; it never contains our token.
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogError(
            "Keycloak Admin API {Method} {Path} failed with {Status}: {Body}",
            response.RequestMessage?.Method, response.RequestMessage?.RequestUri?.AbsolutePath, (int)response.StatusCode, body);

        throw new KeycloakAdminException($"Keycloak Admin API returned {(int)response.StatusCode}.");
    }
}
