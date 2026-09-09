using System.Security.Claims;
using MfaCrud.Api.Auth;
using MfaCrud.Api.Keycloak;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MfaCrud.Api.Account;

public record MeResponse(Guid Id, string? Email, string? Username, string[] Roles);

public record TwoFactorDevice(string Id, string? Label, DateTime CreatedAt);

public record TwoFactorStatus(bool Enabled, IReadOnlyList<TwoFactorDevice> Devices);

public static class AccountEndpoints
{
    public static RouteGroupBuilder MapAccountEndpoints(this RouteGroupBuilder group)
    {
        var auth = group.MapGroup("/auth").WithTags("Account").RequireAuthorization();

        // Everything comes from the token: no call to Keycloak, no local user table.
        auth.MapGet("/me", (ClaimsPrincipal user) =>
                new MeResponse(user.GetUserId(), user.GetEmail(), user.GetUsername(), user.GetRoles()))
            .WithName("GetMe");

        auth.MapGet("/2fa/status", GetTwoFactorStatus).WithName("GetTwoFactorStatus");
        auth.MapDelete("/2fa/{credentialId}", DeleteTwoFactorDevice).WithName("DeleteTwoFactorDevice");

        return group;
    }

    /// <summary>Reads the caller's own OTP credentials from Keycloak. Enrolment happens on Keycloak's pages.</summary>
    private static async Task<Ok<TwoFactorStatus>> GetTwoFactorStatus(
        ClaimsPrincipal user, KeycloakAdminClient keycloak, CancellationToken cancellationToken)
    {
        var devices = await GetOtpDevicesAsync(user.GetUserId(), keycloak, cancellationToken);
        return TypedResults.Ok(new TwoFactorStatus(devices.Count > 0, devices));
    }

    /// <summary>
    /// Removes one of the caller's own authenticators. Only credentials that belong to the
    /// token's subject and are of type otp can be deleted here.
    /// </summary>
    private static async Task<Results<NoContent, NotFound>> DeleteTwoFactorDevice(
        string credentialId, ClaimsPrincipal user, KeycloakAdminClient keycloak, CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        var credentials = await keycloak.GetCredentialsAsync(userId, cancellationToken);

        if (!credentials.Any(credential => credential.IsOtp && credential.Id == credentialId))
        {
            return TypedResults.NotFound();
        }

        var deleted = await keycloak.DeleteCredentialAsync(userId, credentialId, cancellationToken);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static async Task<List<TwoFactorDevice>> GetOtpDevicesAsync(
        Guid userId, KeycloakAdminClient keycloak, CancellationToken cancellationToken)
    {
        var credentials = await keycloak.GetCredentialsAsync(userId, cancellationToken);
        return credentials
            .Where(credential => credential.IsOtp)
            .Select(credential => new TwoFactorDevice(credential.Id, credential.UserLabel, credential.CreatedAt))
            .ToList();
    }
}
