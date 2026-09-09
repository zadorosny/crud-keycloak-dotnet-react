using System.Text.Json.Serialization;

namespace MfaCrud.Api.Keycloak;

/// <summary>Subset of Keycloak's UserRepresentation the API actually uses.</summary>
public sealed record KeycloakUser
{
    public required string Id { get; init; }
    public string? Username { get; init; }
    public string? Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public bool Enabled { get; init; }

    /// <summary>Unix time in milliseconds.</summary>
    public long? CreatedTimestamp { get; init; }

    public DateTime CreatedAt => CreatedTimestamp is { } ms
        ? DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime
        : default;
}

/// <summary>Subset of Keycloak's CredentialRepresentation.</summary>
public sealed record KeycloakCredential
{
    public const string OtpType = "otp";

    public required string Id { get; init; }
    public required string Type { get; init; }
    public string? UserLabel { get; init; }

    /// <summary>Unix time in milliseconds.</summary>
    public long? CreatedDate { get; init; }

    public bool IsOtp => string.Equals(Type, OtpType, StringComparison.OrdinalIgnoreCase);

    public DateTime CreatedAt => CreatedDate is { } ms
        ? DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime
        : default;
}

/// <summary>The Admin API needs both the id and the name to add or remove a role mapping.</summary>
public sealed record KeycloakRole
{
    public required string Id { get; init; }
    public required string Name { get; init; }
}

internal sealed record ServiceTokenResponse
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }
}
