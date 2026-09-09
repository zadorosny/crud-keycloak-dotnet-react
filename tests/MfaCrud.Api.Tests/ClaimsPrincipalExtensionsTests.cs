using System.Security.Claims;
using MfaCrud.Api.Auth;

namespace MfaCrud.Api.Tests;

/// <summary>Unit tests: no containers, no HTTP — only how the API reads a Keycloak token.</summary>
public class ClaimsPrincipalExtensionsTests
{
    private static ClaimsPrincipal PrincipalWith(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "Test"));

    [Fact]
    public void GetUserId_reads_the_sub_claim()
    {
        var sub = Guid.NewGuid();
        var user = PrincipalWith(new Claim("sub", sub.ToString()));

        Assert.Equal(sub, user.GetUserId());
    }

    [Fact]
    public void GetUserId_throws_when_sub_is_missing_or_not_a_guid()
    {
        Assert.Throws<InvalidOperationException>(() => PrincipalWith().GetUserId());
        Assert.Throws<InvalidOperationException>(() => PrincipalWith(new Claim("sub", "nope")).GetUserId());
    }

    [Fact]
    public void GetRoles_returns_every_roles_claim()
    {
        var user = PrincipalWith(
            new Claim(ClaimsPrincipalExtensions.RolesClaim, "admin"),
            new Claim(ClaimsPrincipalExtensions.RolesClaim, "customer"));

        Assert.Equal(["admin", "customer"], user.GetRoles());
    }

    [Fact]
    public void GetRoles_is_empty_when_the_token_carries_none()
    {
        Assert.Empty(PrincipalWith(new Claim("sub", Guid.NewGuid().ToString())).GetRoles());
    }

    [Fact]
    public void Email_and_username_come_from_the_token()
    {
        var user = PrincipalWith(
            new Claim("email", "staff@test.local"),
            new Claim("preferred_username", "staff@test.local"));

        Assert.Equal("staff@test.local", user.GetEmail());
        Assert.Equal("staff@test.local", user.GetUsername());
    }
}
