using System.Security.Claims;
using MfaCrud.Api.Auth;

namespace MfaCrud.Api.Account;

public record MeResponse(Guid Id, string? Email, string? Username, string[] Roles);

public static class AccountEndpoints
{
    public static RouteGroupBuilder MapAccountEndpoints(this RouteGroupBuilder group)
    {
        var auth = group.MapGroup("/auth").WithTags("Account").RequireAuthorization();

        // Everything comes from the token: no call to Keycloak, no local user table.
        auth.MapGet("/me", (ClaimsPrincipal user) =>
                new MeResponse(user.GetUserId(), user.GetEmail(), user.GetUsername(), user.GetRoles()))
            .WithName("GetMe");

        return group;
    }
}
