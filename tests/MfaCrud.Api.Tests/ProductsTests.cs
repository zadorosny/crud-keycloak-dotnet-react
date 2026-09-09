using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MfaCrud.Api.Tests.Infrastructure;

namespace MfaCrud.Api.Tests;

[Collection(ApiCollection.Name)]
public class ProductsTests(ApiFixture fixture)
{
    private static readonly object SampleProduct =
        new { name = "Teclado", description = "60%", price = 349.90m, stock = 7 };

    [Fact]
    public async Task Customer_cannot_create_a_product()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync("customer@test.local", "Customer123!");

        using var response = await client.PostAsJsonAsync("/api/v1/products", SampleProduct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Staff_creates_a_product_and_is_recorded_as_the_author()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync("staff@test.local", "Staff123!");
        var me = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");

        using var response = await client.PostAsJsonAsync("/api/v1/products", SampleProduct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Teclado", created.GetProperty("name").GetString());
        Assert.Equal(349.90m, created.GetProperty("price").GetDecimal());
        Assert.Equal(me.GetProperty("id").GetGuid(), created.GetProperty("createdById").GetGuid());
        Assert.Equal($"/api/v1/products/{created.GetProperty("id").GetGuid()}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Staff_updates_a_product()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync("staff@test.local", "Staff123!");
        var id = await CreateProductAsync(client, "Mouse");

        using var response = await client.PutAsJsonAsync(
            $"/api/v1/products/{id}",
            new { name = "Mouse sem fio", description = (string?)null, price = 199m, stock = 3 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Mouse sem fio", updated.GetProperty("name").GetString());
        Assert.Equal(3, updated.GetProperty("stock").GetInt32());
    }

    [Fact]
    public async Task Customer_cannot_delete_a_product_but_admin_can()
    {
        using var staff = await fixture.CreateAuthenticatedClientAsync("staff@test.local", "Staff123!");
        var id = await CreateProductAsync(staff, "Monitor");

        using var customer = await fixture.CreateAuthenticatedClientAsync("customer@test.local", "Customer123!");
        using var forbidden = await customer.DeleteAsync($"/api/v1/products/{id}");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        using var admin = await fixture.CreateAuthenticatedClientAsync("admin@test.local", "Admin123!");
        using var deleted = await admin.DeleteAsync($"/api/v1/products/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        using var gone = await admin.GetAsync($"/api/v1/products/{id}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    [Fact]
    public async Task Any_authenticated_user_reads_the_catalog_with_paging_and_search()
    {
        using var staff = await fixture.CreateAuthenticatedClientAsync("staff@test.local", "Staff123!");
        var marker = $"Cadeira {Guid.NewGuid():N}";
        await CreateProductAsync(staff, marker);

        using var customer = await fixture.CreateAuthenticatedClientAsync("customer@test.local", "Customer123!");
        var page = await customer.GetFromJsonAsync<JsonElement>(
            $"/api/v1/products?page=1&pageSize=5&search={marker.ToLowerInvariant()}");

        Assert.Equal(1, page.GetProperty("total").GetInt32());
        Assert.Equal(1, page.GetProperty("page").GetInt32());
        Assert.Equal(5, page.GetProperty("pageSize").GetInt32());
        Assert.Equal(marker, page.GetProperty("items")[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task Page_size_is_capped()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync("customer@test.local", "Customer123!");

        var page = await client.GetFromJsonAsync<JsonElement>("/api/v1/products?pageSize=5000");

        Assert.Equal(100, page.GetProperty("pageSize").GetInt32());
    }

    [Fact]
    public async Task Invalid_payload_is_rejected()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync("staff@test.local", "Staff123!");

        using var response = await client.PostAsJsonAsync(
            "/api/v1/products",
            new { name = "", description = (string?)null, price = -1m, stock = -5 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_product_is_not_found()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync("customer@test.local", "Customer123!");

        using var response = await client.GetAsync($"/api/v1/products/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<Guid> CreateProductAsync(HttpClient client, string name)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/products",
            new { name, description = "fixture", price = 10m, stock = 1 });
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        return created.GetProperty("id").GetGuid();
    }
}
