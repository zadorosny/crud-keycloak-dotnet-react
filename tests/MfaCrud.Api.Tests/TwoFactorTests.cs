using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MfaCrud.Api.Tests.Infrastructure;

namespace MfaCrud.Api.Tests;

[Collection(ApiCollection.Name)]
public class TwoFactorTests(ApiFixture fixture)
{
    [Fact]
    public async Task Status_lists_the_authenticator_of_the_user_who_has_one()
    {
        using var client = await fixture.CreateTwoFactorClientAsync();

        var status = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/2fa/status");

        Assert.True(status.GetProperty("enabled").GetBoolean());
        var devices = status.GetProperty("devices").EnumerateArray().ToList();
        var device = Assert.Single(devices);
        Assert.Equal("test-authenticator", device.GetProperty("label").GetString());
        Assert.False(string.IsNullOrWhiteSpace(device.GetProperty("id").GetString()));
    }

    [Fact]
    public async Task Status_is_disabled_for_a_user_without_an_authenticator()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync("customer@test.local", "Customer123!");

        var status = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/2fa/status");

        Assert.False(status.GetProperty("enabled").GetBoolean());
        Assert.Empty(status.GetProperty("devices").EnumerateArray());
    }

    [Fact]
    public async Task A_credential_that_belongs_to_someone_else_cannot_be_deleted()
    {
        using var owner = await fixture.CreateTwoFactorClientAsync();
        var status = await owner.GetFromJsonAsync<JsonElement>("/api/v1/auth/2fa/status");
        var credentialId = status.GetProperty("devices")[0].GetProperty("id").GetString();

        using var someoneElse = await fixture.CreateAuthenticatedClientAsync("customer@test.local", "Customer123!");
        using var response = await someoneElse.DeleteAsync($"/api/v1/auth/2fa/{credentialId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // The authenticator is still there.
        var stillEnabled = await owner.GetFromJsonAsync<JsonElement>("/api/v1/auth/2fa/status");
        Assert.True(stillEnabled.GetProperty("enabled").GetBoolean());
    }

    [Fact]
    public async Task An_unknown_credential_is_not_found()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync("customer@test.local", "Customer123!");

        using var response = await client.DeleteAsync($"/api/v1/auth/2fa/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Status_requires_a_token()
    {
        using var client = fixture.Factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/auth/2fa/status");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
