using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using OtpNet;

namespace MfaCrud.Api.Tests.Infrastructure;

/// <summary>
/// Gets tokens straight from Keycloak with the direct grant of the dev client, the same way newman
/// does. The API is never asked for a token.
/// </summary>
public sealed class TokenClient(string authority) : IDisposable
{
    public const string DevClientId = "mfacrud-postman";

    /// <summary>Secret of the OTP credential imported for customer2fa@test.local.</summary>
    public const string TotpSecret = "PORTFOLIO2FASECRET20";

    private readonly HttpClient _http = new();

    /// <summary>Keycloak uses the UTF-8 bytes of the secret as the HMAC key.</summary>
    public static string CurrentTotp() => new Totp(Encoding.UTF8.GetBytes(TotpSecret)).ComputeTotp();

    public async Task<HttpResponseMessage> RequestTokenAsync(
        string username, string password, string? totp = null, string clientId = DevClientId)
    {
        var form = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["grant_type"] = "password",
            ["username"] = username,
            ["password"] = password,
        };

        if (totp is not null)
        {
            form["totp"] = totp;
        }

        return await _http.PostAsync($"{authority}/protocol/openid-connect/token", new FormUrlEncodedContent(form));
    }

    public async Task<string> GetTokenAsync(
        string username, string password, string? totp = null, string clientId = DevClientId)
    {
        using var response = await RequestTokenAsync(username, password, totp, clientId);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return payload?.AccessToken ?? throw new InvalidOperationException("Keycloak returned no access token.");
    }

    public void Dispose() => _http.Dispose();

    private sealed record TokenResponse([property: JsonPropertyName("access_token")] string AccessToken);
}
