using System;

namespace Application.DTOs.User.Response;

public sealed class UserResponse(
    Guid userId,
    string email,
    string username,
    string role,
    string? phone,
    Guid? createdByOwnerId,
    bool isActive)
{
    public Guid UserId { get; init; } = userId;
    public string Email { get; init; } = email;
    public string Username { get; init; } = username;
    public string Role { get; init; } = role;
    public string? PhoneNumber { get; init; } = phone;
    public Guid? CreatedByOwnerId { get; init; } = createdByOwnerId;
    public bool IsActive { get; init; } = isActive;
}
