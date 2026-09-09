using MfaCrud.Api.Account;
using MfaCrud.Api.Auth;
using MfaCrud.Api.Common;
using MfaCrud.Api.Data;
using MfaCrud.Api.Keycloak;
using MfaCrud.Api.Products;
using MfaCrud.Api.Telemetry;
using MfaCrud.Api.Users;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddMfaCrudTelemetry();

const string WebAppCors = "web-app";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

// A connection string é resolvida quando o serviço é construído, e não aqui: assim quem hospeda a
// API (os testes de integração, com o Postgres em container) consegue sobrescrevê-la.
static string ConnectionString(IServiceProvider services) =>
    services.GetRequiredService<IConfiguration>().GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");

builder.Services.AddDbContext<AppDbContext>((services, options) => options.UseNpgsql(ConnectionString(services)));

builder.Services.AddKeycloakAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddKeycloakAdminApi();

builder.Services.AddCors(options => options.AddPolicy(WebAppCors, policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddHealthChecks()
    .AddNpgSql(ConnectionString, name: "postgres")
    .AddCheck<KeycloakDiscoveryHealthCheck>("keycloak");
builder.Services.AddHttpClient<KeycloakDiscoveryHealthCheck>(http => http.Timeout = TimeSpan.FromSeconds(5));

builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSecurityHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

app.UseCors(WebAppCors);
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

var api = app.MapGroup("/api/v1");
api.MapAccountEndpoints();
api.MapProductEndpoints();
api.MapUserEndpoints();

app.Run();

/// <summary>Exposed so the integration tests can boot the API with WebApplicationFactory.</summary>
public partial class Program;
