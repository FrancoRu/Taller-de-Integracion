using System;

namespace Application.DTOs.Auth.Response;

/// <summary>
/// HU-09: confirmation payload returned after inviting a user by email. The
/// account exists but has no password yet until the user completes activation.
/// </summary>
public sealed class InviteUserResponse(Guid userId, string email, string role)
{
    public Guid UserId { get; init; } = userId;
    public string Email { get; init; } = email;
    public string Role { get; init; } = role;
}
