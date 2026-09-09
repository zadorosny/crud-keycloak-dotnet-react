using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
using MfaCrud.Api.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;

namespace MfaCrud.Api.Tests;

[Collection(ApiCollection.Name)]
public class TelemetryTests(ApiFixture fixture)
{
    [Fact]
    public async Task Writing_a_product_counts_the_operation()
    {
        var meterFactory = fixture.Factory.Services.GetRequiredService<IMeterFactory>();
        using var written = new MetricCollector<long>(meterFactory, "MfaCrud.Api", "mfacrud.products.written");

        using var client = await fixture.CreateAuthenticatedClientAsync("staff@test.local", "Staff123!");
        using var response = await client.PostAsJsonAsync(
            "/api/v1/products",
            new { name = $"Telemetria {Guid.NewGuid():N}", description = (string?)null, price = 1m, stock = 1 });
        response.EnsureSuccessStatusCode();

        var measurement = Assert.Single(written.GetMeasurementSnapshot());
        Assert.Equal(1, measurement.Value);
        Assert.Equal("created", measurement.Tags["operation"]);
    }

    [Fact]
    public async Task A_request_produces_a_server_span()
    {
        var spans = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Microsoft.AspNetCore",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => spans.Add(activity),
        };
        ActivitySource.AddActivityListener(listener);

        using var client = await fixture.CreateAuthenticatedClientAsync("customer@test.local", "Customer123!");
        using var response = await client.GetAsync("/api/v1/products");
        response.EnsureSuccessStatusCode();

        var span = Assert.Single(spans, activity => activity.DisplayName.Contains("/api/v1/products"));
        Assert.Equal(ActivityKind.Server, span.Kind);
        Assert.Equal("GET", span.GetTagItem("http.request.method"));
        Assert.Equal(200, Convert.ToInt32(span.GetTagItem("http.response.status_code")));
    }

    [Fact]
    public async Task Health_checks_are_not_traced()
    {
        var spans = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Microsoft.AspNetCore",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => spans.Add(activity),
        };
        ActivitySource.AddActivityListener(listener);

        using var client = fixture.Factory.CreateClient();
        using var response = await client.GetAsync("/health");
        response.EnsureSuccessStatusCode();

        // O filtro da instrumentação existe para o healthcheck do compose não poluir os traces.
        Assert.DoesNotContain(spans, activity => activity.DisplayName.Contains("/health"));
    }

    [Fact]
    public async Task Removing_an_authenticator_is_only_counted_when_it_happens()
    {
        var meterFactory = fixture.Factory.Services.GetRequiredService<IMeterFactory>();
        using var removed = new MetricCollector<long>(meterFactory, "MfaCrud.Api", "mfacrud.two_factor.devices_removed");

        using var client = await fixture.CreateAuthenticatedClientAsync("customer@test.local", "Customer123!");
        using var response = await client.DeleteAsync($"/api/v1/auth/2fa/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(removed.GetMeasurementSnapshot());
    }
}
