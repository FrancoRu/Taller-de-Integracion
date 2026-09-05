using System;

namespace Application.DTOs.Auth.Response;

/// <summary>
/// Confirmation payload returned after inviting a user by email; the account has no password until activation.
/// </summary>
public sealed class InviteUserResponse(Guid userId, string email, string role)
{
    public Guid UserId { get; init; } = userId;
    public string Email { get; init; } = email;
    public string Role { get; init; } = role;
}
