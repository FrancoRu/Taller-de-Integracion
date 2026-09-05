using Application.DTOs.Abstract.Response;
using Application.DTOs.User.Request;
using Application.DTOs.User.Response;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// CRUD operations on Identity users, with role-based access enforcement.
/// </summary>
public interface IUserManagementService
{
    /// <summary>
    /// ADMIN sees all users; OWNER sees only their own subordinates; others get 403.
    /// </summary>
    Task<PaginatedResponse<UserResponse>> GetAllAsync(
        string callerRole, Guid callerId,
        UserFilteredRequest filter,
        CancellationToken ct = default);

    /// <summary>
    /// ADMIN → any user. OWNER → self or their subordinates. Others → self only.
    /// </summary>
    Task<UserResponse> GetByIdAsync(
        string callerRole, Guid callerId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Updates profile fields including username, email, and phone, where ADMIN can update any user, OWNER can update self or their subordinates, and others only themselves.
    /// </summary>
    Task<UserResponse> UpdateAsync(
        string callerRole, Guid callerId, Guid userId,
        UpdateUserRequest request, CancellationToken ct = default);

    /// <summary>
    /// Changes a user's password, self-service or privileged, where ADMIN can change any user, OWNER can change self or their subordinates, and others only themselves.
    /// </summary>
    Task ChangePasswordAsync(
        string callerRole, Guid callerId, Guid userId,
        ChangePasswordRequest request, CancellationToken ct = default);

    /// <summary>
    /// Forces a password reset by generating a temporary password, where ADMIN can reset any user and OWNER only their own subordinates; others get 403.
    /// </summary>
    Task<ResetPasswordResponse> ResetPasswordAsync(
        string callerRole, Guid callerId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// ADMIN → any user. OWNER → their subordinates only. Others → 403.
    /// </summary>
    Task DeleteAsync(
        string callerRole, Guid callerId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Activates or deactivates a user account via Identity lockout, where ADMIN can act on any user and OWNER only their subordinates; others get 403.
    /// </summary>
    Task<UserResponse> SetActiveAsync(
        string callerRole, Guid callerId, Guid userId, bool isActive,
        CancellationToken ct = default);
}