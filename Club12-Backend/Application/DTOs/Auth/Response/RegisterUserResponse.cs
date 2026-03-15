using System;

namespace Application.DTOs.Auth.Response;

/// <summary>
/// Confirmation payload returned after successfully registering a new user.
/// </summary>
public sealed class RegisterUserResponse(Guid userId, string email, string role)
{
    public Guid   UserId { get; init; } = userId;
    public string Email  { get; init; } = email;
    public string Role   { get; init; } = role;
}