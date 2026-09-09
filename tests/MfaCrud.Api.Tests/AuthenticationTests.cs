using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MfaCrud.Api.Tests.Infrastructure;

namespace MfaCrud.Api.Tests;

[Collection(ApiCollection.Name)]
public class AuthenticationTests(ApiFixture fixture)
{
    [Fact]
    public async Task Request_without_token_is_unauthorized()
    {
        using var client = fixture.Factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Token_issued_for_another_audience_is_unauthorized()
    {
        // admin-cli is a built-in client without the mfacrud-api audience mapper.
        var token = await fixture.Tokens.GetTokenAsync("admin@test.local", "Admin123!", clientId: "admin-cli");
        using var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.GetAsync("/api/v1/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Garbage_token_is_unauthorized()
    {
        using var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-jwt");

        using var response = await client.GetAsync("/api/v1/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("admin@test.local", "Admin123!", "admin")]
    [InlineData("staff@test.local", "Staff123!", "staff")]
    [InlineData("customer@test.local", "Customer123!", "customer")]
    public async Task Me_returns_the_identity_carried_by_the_token(string username, string password, string role)
    {
        using var client = await fixture.CreateAuthenticatedClientAsync(username, password);

        var me = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");

        Assert.Equal(username, me.GetProperty("username").GetString());
        Assert.Equal(username, me.GetProperty("email").GetString());
        Assert.NotEqual(Guid.Empty, me.GetProperty("id").GetGuid());
        Assert.Contains(role, me.GetProperty("roles").EnumerateArray().Select(r => r.GetString()));
    }

    [Fact]
    public async Task Second_factor_is_required_only_for_the_user_who_configured_it()
    {
        // customer@test.local has no authenticator: the password alone is enough.
        using var withoutOtp = await fixture.Tokens.RequestTokenAsync("customer@test.local", "Customer123!");
        Assert.True(withoutOtp.IsSuccessStatusCode);

        // customer2fa@test.local has one: the password alone gets no token.
        using var denied = await fixture.Tokens.RequestTokenAsync("customer2fa@test.local", "Customer123!");
        Assert.False(denied.IsSuccessStatusCode);

        // With the TOTP code it does, and the API accepts the token.
        using var client = await fixture.CreateAuthenticatedClientAsync(
            "customer2fa@test.local", "Customer123!", TokenClient.CurrentTotp());
        using var response = await client.GetAsync("/api/v1/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
