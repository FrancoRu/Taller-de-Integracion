using System;

namespace Application.DTOs.Auth.Response;

/// <summary>
/// Confirmation payload returned after successfully registering a new user.
/// </summary>
public sealed class RegisterUserResponse(Guid userId, string email, string username, string role, string? phone)
{
    public Guid   UserId   { get; init; } = userId;
    public string Email    { get; init; } = email;
    public string Username { get; init; } = username;
    public string Role     { get; init; } = role;
    public string? PhoneNumber { get; init; } = phone;
}