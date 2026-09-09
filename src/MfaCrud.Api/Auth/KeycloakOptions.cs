namespace MfaCrud.Api.Auth;

public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    /// <summary>Realm URL used for OIDC discovery, e.g. http://localhost:8080/realms/mfacrud.</summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>Client the access token must be addressed to.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Expected <c>iss</c>. Only differs from <see cref="Authority"/> when the API reaches
    /// Keycloak through a different host than the browser does (see M6).</summary>
    public string? Issuer { get; set; }
}
