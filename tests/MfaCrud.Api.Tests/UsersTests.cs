using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MfaCrud.Api.Tests.Infrastructure;

namespace MfaCrud.Api.Tests;

[Collection(ApiCollection.Name)]
public class UsersTests(ApiFixture fixture)
{
    [Fact]
    public async Task Admin_lists_users_and_sees_who_has_a_second_factor()
    {
        using var admin = await fixture.CreateAuthenticatedClientAsync("admin@test.local", "Admin123!");

        var page = await admin.GetFromJsonAsync<JsonElement>("/api/v1/users?first=0&max=20");

        var users = page.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(0, page.GetProperty("first").GetInt32());
        Assert.Equal(20, page.GetProperty("max").GetInt32());

        var withOtp = users.Single(u => u.GetProperty("username").GetString() == ApiFixture.TwoFactorUser);
        Assert.True(withOtp.GetProperty("twoFactorEnabled").GetBoolean());
        Assert.Contains("customer", withOtp.GetProperty("roles").EnumerateArray().Select(r => r.GetString()));

        var withoutOtp = users.Single(u => u.GetProperty("username").GetString() == "staff@test.local");
        Assert.False(withoutOtp.GetProperty("twoFactorEnabled").GetBoolean());
        Assert.Contains("staff", withoutOtp.GetProperty("roles").EnumerateArray().Select(r => r.GetString()));
        Assert.True(withoutOtp.GetProperty("enabled").GetBoolean());
        Assert.NotEqual(default, withoutOtp.GetProperty("createdAt").GetDateTime());
    }

    [Fact]
    public async Task Admin_searches_and_reads_a_single_user()
    {
        using var admin = await fixture.CreateAuthenticatedClientAsync("admin@test.local", "Admin123!");

        var page = await admin.GetFromJsonAsync<JsonElement>("/api/v1/users?search=staff@test.local");
        var id = page.GetProperty("items")[0].GetProperty("id").GetGuid();

        var user = await admin.GetFromJsonAsync<JsonElement>($"/api/v1/users/{id}");

        Assert.Equal("staff@test.local", user.GetProperty("email").GetString());
        Assert.Equal("Sam", user.GetProperty("firstName").GetString());
    }

    [Fact]
    public async Task Unknown_user_is_not_found()
    {
        using var admin = await fixture.CreateAuthenticatedClientAsync("admin@test.local", "Admin123!");

        using var response = await admin.GetAsync($"/api/v1/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("staff@test.local", "Staff123!")]
    [InlineData("customer@test.local", "Customer123!")]
    public async Task Only_admins_reach_the_user_area(string username, string password)
    {
        using var client = await fixture.CreateAuthenticatedClientAsync(username, password);

        using var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Promoting_a_customer_to_staff_lets_the_next_token_create_products()
    {
        using var admin = await fixture.CreateAuthenticatedClientAsync("admin@test.local", "Admin123!");
        var id = await FindUserIdAsync(admin, "customer@test.local");

        using (var before = await fixture.CreateAuthenticatedClientAsync("customer@test.local", "Customer123!"))
        {
            using var denied = await before.PostAsJsonAsync("/api/v1/products", NewProduct());
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        }

        try
        {
            using var promoted = await admin.PutAsJsonAsync($"/api/v1/users/{id}/roles", new { roles = new[] { "staff" } });

            Assert.Equal(HttpStatusCode.OK, promoted.StatusCode);
            var dto = await promoted.Content.ReadFromJsonAsync<JsonElement>();
            var roles = dto.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ToList();
            Assert.Contains("staff", roles);
            Assert.DoesNotContain("customer", roles);

            using var after = await fixture.CreateAuthenticatedClientAsync("customer@test.local", "Customer123!");
            using var created = await after.PostAsJsonAsync("/api/v1/products", NewProduct());
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        }
        finally
        {
            using var restored = await admin.PutAsJsonAsync($"/api/v1/users/{id}/roles", new { roles = new[] { "customer" } });
            restored.EnsureSuccessStatusCode();
        }
    }

    [Fact]
    public async Task An_admin_cannot_drop_their_own_admin_role()
    {
        using var admin = await fixture.CreateAuthenticatedClientAsync("admin@test.local", "Admin123!");
        var me = await admin.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
        var id = me.GetProperty("id").GetGuid();

        using var response = await admin.PutAsJsonAsync($"/api/v1/users/{id}/roles", new { roles = new[] { "staff" } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Still an admin.
        var user = await admin.GetFromJsonAsync<JsonElement>($"/api/v1/users/{id}");
        Assert.Contains("admin", user.GetProperty("roles").EnumerateArray().Select(r => r.GetString()));
    }

    [Fact]
    public async Task Unknown_and_empty_role_sets_are_rejected()
    {
        using var admin = await fixture.CreateAuthenticatedClientAsync("admin@test.local", "Admin123!");
        var id = await FindUserIdAsync(admin, "staff@test.local");

        using var unknown = await admin.PutAsJsonAsync($"/api/v1/users/{id}/roles", new { roles = new[] { "superuser" } });
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);

        using var empty = await admin.PutAsJsonAsync($"/api/v1/users/{id}/roles", new { roles = Array.Empty<string>() });
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
    }

    private static object NewProduct() =>
        new { name = $"Item {Guid.NewGuid():N}", description = "role check", price = 1m, stock = 1 };

    private static async Task<Guid> FindUserIdAsync(HttpClient admin, string username)
    {
        var page = await admin.GetFromJsonAsync<JsonElement>($"/api/v1/users?search={username}");
        return page.GetProperty("items").EnumerateArray()
            .Single(u => u.GetProperty("username").GetString() == username)
            .GetProperty("id").GetGuid();
    }
}
