using System.ComponentModel.DataAnnotations;

namespace MfaCrud.Api.Users;

public record UserDto(
    Guid Id,
    string? Email,
    string? Username,
    string? FirstName,
    string? LastName,
    bool Enabled,
    string[] Roles,
    bool TwoFactorEnabled,
    DateTime CreatedAt);

/// <summary>The Admin API pages by offset, so the API surfaces first/max instead of page/pageSize.</summary>
public record UserPage(IReadOnlyList<UserDto> Items, int First, int Max);

public class UpdateUserRolesRequest
{
    [Required]
    [MinLength(1)]
    public string[] Roles { get; init; } = [];
}
