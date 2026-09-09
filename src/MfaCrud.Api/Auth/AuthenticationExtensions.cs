using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace MfaCrud.Api.Auth;

public static class AuthenticationExtensions
{
    public const string AdminOnly = "AdminOnly";
    public const string StaffOrAdmin = "StaffOrAdmin";

    /// <summary>
    /// Resource server setup: the API only validates tokens minted by Keycloak. It never issues
    /// tokens, checks passwords or handles TOTP.
    /// </summary>
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddOptions<KeycloakOptions>()
            .Bind(configuration.GetSection(KeycloakOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Authority), "Keycloak:Authority is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Audience), "Keycloak:Audience is required.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Configured through IOptions instead of inline: the values must be read when the options
        // are resolved, not while services are being registered, so that whoever hosts the API
        // (the integration tests, for one) can still override the Keycloak settings.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<KeycloakOptions>>((jwt, keycloak) =>
            {
                var options = keycloak.Value;

                jwt.Authority = options.Authority;
                jwt.Audience = options.Audience;
                jwt.RequireHttpsMetadata = !environment.IsDevelopment();
                jwt.MapInboundClaims = false;
                jwt.TokenValidationParameters.NameClaimType = "preferred_username";
                jwt.TokenValidationParameters.RoleClaimType = ClaimsPrincipalExtensions.RolesClaim;
                jwt.TokenValidationParameters.ValidIssuer = options.Issuer ?? options.Authority;
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AdminOnly, policy => policy.RequireRole("admin"))
            .AddPolicy(StaffOrAdmin, policy => policy.RequireRole("admin", "staff"));

        return services;
    }
}
