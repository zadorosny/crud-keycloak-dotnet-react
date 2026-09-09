using MfaCrud.Api.Account;
using MfaCrud.Api.Auth;
using MfaCrud.Api.Data;
using MfaCrud.Api.Keycloak;
using MfaCrud.Api.Products;
using MfaCrud.Api.Users;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

const string WebAppCors = "web-app";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddKeycloakAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddKeycloakAdminApi();

builder.Services.AddCors(options => options.AddPolicy(WebAppCors, policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

app.UseCors(WebAppCors);
app.UseAuthentication();
app.UseAuthorization();

var api = app.MapGroup("/api/v1");
api.MapAccountEndpoints();
api.MapProductEndpoints();
api.MapUserEndpoints();

app.Run();

/// <summary>Exposed so the integration tests can boot the API with WebApplicationFactory.</summary>
public partial class Program;
