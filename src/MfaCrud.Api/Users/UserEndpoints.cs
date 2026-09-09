using System.Collections.Concurrent;
using System.Security.Claims;
using MfaCrud.Api.Auth;
using MfaCrud.Api.Keycloak;
using MfaCrud.Api.Telemetry;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MfaCrud.Api.Users;

public static class UserEndpoints
{
    /// <summary>The only realm roles this API hands out. Anything else is Keycloak's business.</summary>
    private static readonly string[] ManagedRoles = ["admin", "staff", "customer"];

    private const int MaxPageSize = 100;

    /// <summary>Keycloak is asked about several users at once, but never more than this.</summary>
    private const int MaxConcurrentLookups = 5;

    public static RouteGroupBuilder MapUserEndpoints(this RouteGroupBuilder group)
    {
        var users = group.MapGroup("/users")
            .WithTags("Users")
            .RequireAuthorization(AuthenticationExtensions.AdminOnly);

        users.MapGet("", GetUsers).WithName("GetUsers");
        users.MapGet("/{id:guid}", GetUser).WithName("GetUser");
        users.MapPut("/{id:guid}/roles", UpdateUserRoles).WithName("UpdateUserRoles");

        return group;
    }

    private static async Task<Ok<UserPage>> GetUsers(
        KeycloakAdminClient keycloak,
        int first = 0,
        int max = 20,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        first = Math.Max(first, 0);
        max = Math.Clamp(max, 1, MaxPageSize);

        var users = await keycloak.GetUsersAsync(first, max, search, cancellationToken);
        var details = new ConcurrentDictionary<string, UserDto>();

        await Parallel.ForEachAsync(
            users,
            new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrentLookups, CancellationToken = cancellationToken },
            async (user, token) =>
            {
                details[user.Id] = await ToDtoAsync(user, keycloak, token);
            });

        // ConcurrentDictionary does not keep order; Keycloak's order is the one worth showing.
        var items = users.Select(user => details[user.Id]).ToList();
        return TypedResults.Ok(new UserPage(items, first, max));
    }

    private static async Task<Results<Ok<UserDto>, NotFound>> GetUser(
        Guid id, KeycloakAdminClient keycloak, CancellationToken cancellationToken)
    {
        var user = await keycloak.GetUserAsync(id, cancellationToken);
        return user is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(await ToDtoAsync(user, keycloak, cancellationToken));
    }

    /// <summary>Replaces the set of managed roles the user holds. Roles outside that set are untouched.</summary>
    private static async Task<Results<Ok<UserDto>, NotFound, ValidationProblem>> UpdateUserRoles(
        Guid id,
        UpdateUserRolesRequest request,
        ClaimsPrincipal caller,
        KeycloakAdminClient keycloak,
        MfaCrudTelemetry telemetry,
        CancellationToken cancellationToken)
    {
        var requested = request.Roles.Distinct(StringComparer.Ordinal).ToArray();

        var unknown = requested.Where(role => !ManagedRoles.Contains(role, StringComparer.Ordinal)).ToArray();
        if (unknown.Length > 0)
        {
            return Invalid($"Unknown role(s): {string.Join(", ", unknown)}. Allowed: {string.Join(", ", ManagedRoles)}.");
        }

        if (requested.Length == 0)
        {
            return Invalid("At least one role is required.");
        }

        // An admin locking themselves out of the admin area is almost never what they meant.
        if (id == caller.GetUserId() && !requested.Contains("admin", StringComparer.Ordinal))
        {
            return Invalid("You cannot remove your own admin role.");
        }

        var user = await keycloak.GetUserAsync(id, cancellationToken);
        if (user is null)
        {
            return TypedResults.NotFound();
        }

        var current = await keycloak.GetUserRealmRolesAsync(id, cancellationToken);
        var currentManaged = current.Where(role => ManagedRoles.Contains(role.Name, StringComparer.Ordinal)).ToList();

        var toRemove = currentManaged.Where(role => !requested.Contains(role.Name, StringComparer.Ordinal)).ToList();
        if (toRemove.Count > 0)
        {
            await keycloak.RemoveRealmRolesAsync(id, toRemove, cancellationToken);
        }

        var missing = requested.Where(role => !currentManaged.Any(held => held.Name == role)).ToList();
        if (missing.Count > 0)
        {
            var toAdd = new List<KeycloakRole>(missing.Count);
            foreach (var name in missing)
            {
                var role = await keycloak.GetRealmRoleAsync(name, cancellationToken)
                    ?? throw new KeycloakAdminException($"Realm role '{name}' does not exist in Keycloak.");
                toAdd.Add(role);
            }

            await keycloak.AddRealmRolesAsync(id, toAdd, cancellationToken);
        }

        foreach (var role in requested)
        {
            telemetry.RoleChanged(role);
        }

        return TypedResults.Ok(await ToDtoAsync(user, keycloak, cancellationToken));
    }

    private static ValidationProblem Invalid(string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["roles"] = [message] });

    private static async Task<UserDto> ToDtoAsync(
        KeycloakUser user, KeycloakAdminClient keycloak, CancellationToken cancellationToken)
    {
        var id = Guid.Parse(user.Id);
        var roles = await keycloak.GetUserRealmRolesAsync(id, cancellationToken);
        var credentials = await keycloak.GetCredentialsAsync(id, cancellationToken);

        return new UserDto(
            id,
            user.Email,
            user.Username,
            user.FirstName,
            user.LastName,
            user.Enabled,
            roles.Select(role => role.Name).Order(StringComparer.Ordinal).ToArray(),
            credentials.Any(credential => credential.IsOtp),
            user.CreatedAt);
    }
}
