using System.Security.Claims;

namespace MfaCrud.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public const string RolesClaim = "roles";

    /// <summary>Keycloak <c>sub</c>, the stable user id.</summary>
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"), out var id)
            ? id
            : throw new InvalidOperationException("Access token has no usable 'sub' claim.");

    public static string? GetEmail(this ClaimsPrincipal user) => user.FindFirstValue("email");

    public static string? GetUsername(this ClaimsPrincipal user) => user.FindFirstValue("preferred_username");

    public static string[] GetRoles(this ClaimsPrincipal user) =>
        user.FindAll(RolesClaim).Select(c => c.Value).ToArray();
}
