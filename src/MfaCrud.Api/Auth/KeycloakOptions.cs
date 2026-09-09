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

    /// <summary>Keycloak root URL, e.g. http://localhost:8080. Used for the Admin REST API.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    public string Realm { get; set; } = "mfacrud";

    /// <summary>Confidential client whose service account talks to the Admin REST API.</summary>
    public string AdminClientId { get; set; } = "mfacrud-admin-svc";

    /// <summary>Comes from user-secrets or the environment, never from appsettings.</summary>
    public string AdminClientSecret { get; set; } = string.Empty;
}
