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
    /// ADMIN → all users. OWNER → only their own subordinates. Others → 403.
    /// Supports pagination and filtering via <paramref name="filter"/>.
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
    /// Updates profile fields (username, email, phone).
    /// ADMIN → any user. OWNER → self or their subordinates. Others → self only.
    /// If <see cref="UpdateUserRequest.Role"/> is set, additionally changes the
    /// target's role (replacing whatever role they currently have). Role changes
    /// are restricted to ADMIN (any role, any user) and OWNER (TOURNAMENT_MANAGER
    /// only, and only for their own subordinates); nobody may change their own role.
    /// </summary>
    Task<UserResponse> UpdateAsync(
        string callerRole, Guid callerId, Guid userId,
        UpdateUserRequest request, CancellationToken ct = default);

    /// <summary>
    /// Changes a user's password (self-service or privileged).
    /// ADMIN → any user. OWNER → self or their subordinates. Others → self only.
    /// </summary>
    Task ChangePasswordAsync(
        string callerRole, Guid callerId, Guid userId,
        ChangePasswordRequest request, CancellationToken ct = default);

    /// <summary>
    /// Forces a password reset, generating a temporary password.
    /// ADMIN → any user. OWNER → only their own subordinates. Others → 403.
    /// </summary>
    Task<ResetPasswordResponse> ResetPasswordAsync(
        string callerRole, Guid callerId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// ADMIN → any user. OWNER → their subordinates only. Others → 403.
    /// </summary>
    Task DeleteAsync(
        string callerRole, Guid callerId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Activates or deactivates a user account via Identity lockout.
    /// A deactivated account cannot log in until reactivated.
    /// ADMIN → any user. OWNER → their subordinates only. Others → 403.
    /// </summary>
    Task<UserResponse> SetActiveAsync(
        string callerRole, Guid callerId, Guid userId, bool isActive,
        CancellationToken ct = default);
}