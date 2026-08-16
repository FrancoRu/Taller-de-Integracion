using Application.DTOs.Abstract.Request;
using Application.DTOs.Abstract.Response;
using Application.DTOs.User.Request;
using Application.DTOs.User.Response;
using Application.Interfaces.Services;
using Application.Utils.Constants;
using Application.Utils.Constants.Configuration;

using Domain.Enums;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Identity;

/// <summary>
/// Identity-backed implementation of IUserManagementService.
/// All access-control rules are enforced here, keeping controllers thin.
/// </summary>
public sealed class IdentityUserManagementService(
    UserManager<ApplicationUser> userManager,
    IdentityAppDbContext identityDbContext,
    IEmailService emailService,
    IConfiguration configuration) : IUserManagementService
{
    public async Task<PaginatedResponse<UserResponse>> GetAllAsync(
        string callerRole, Guid callerId,
        UserFilteredRequest filter,
        CancellationToken ct = default)
    {
        IQueryable<ApplicationUser> query = ResolveVisibleUsersQuery(callerRole, callerId);

        if (!string.IsNullOrWhiteSpace(filter.UserName))
        {
            query = query.Where(u => u.UserName != null &&
                                     u.UserName.ToLower().Contains(filter.UserName.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(filter.Email))
        {
            query = query.Where(u => u.Email != null &&
                                     u.Email.ToLower().Contains(filter.Email.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(filter.PhoneNumber))
        {
            query = query.Where(u => u.PhoneNumber != null &&
                                     u.PhoneNumber.Contains(filter.PhoneNumber));
        }

        if (filter.Role.HasValue)
        {
            string roleName = filter.Role.Value.ToRoleName();

            IQueryable<Guid> userIdsWithRole =
                from ur in identityDbContext.UserRoles
                join r in identityDbContext.Roles on ur.RoleId equals r.Id
                where r.Name == roleName
                select ur.UserId;

            query = query.Where(u => userIdsWithRole.Contains(u.Id));
        }

        int totalCount = await query.CountAsync(ct);

        query = filter.OrderBy?.ToLower() switch
        {
            "username" => filter.Order == SortOrder.Descending
                ? query.OrderByDescending(u => u.UserName)
                : query.OrderBy(u => u.UserName),

            "email" => filter.Order == SortOrder.Descending
                ? query.OrderByDescending(u => u.Email)
                : query.OrderBy(u => u.Email),

            _ => filter.Order == SortOrder.Descending
                ? query.OrderByDescending(u => u.DateCreated)
                : query.OrderBy(u => u.DateCreated),
        };

        int skip = (filter.PageNumber - 1) * filter.PageSize;
        List<ApplicationUser> users = await query.Skip(skip).Take(filter.PageSize).ToListAsync(ct);

        IReadOnlyList<UserResponse> items = await MapManyAsync(users);

        return new PaginatedResponse<UserResponse>
        {
            Items = items,
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<UserResponse> GetByIdAsync(
        string callerRole, Guid callerId, Guid userId, CancellationToken ct = default)
    {
        ApplicationUser user = await FindOrThrowAsync(userId);
        EnforceReadAccess(user, callerRole, callerId);
        return await MapOneAsync(user);
    }

    public async Task<UserResponse> UpdateAsync(
        string callerRole, Guid callerId, Guid userId,
        UpdateUserRequest request, CancellationToken ct = default)
    {
        ApplicationUser user = await FindOrThrowAsync(userId);
        EnforceWriteAccess(user, callerRole, callerId);

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            ThrowIfFailed(await userManager.SetUserNameAsync(user, request.Username));
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            ThrowIfFailed(await userManager.SetEmailAsync(user, request.Email));
        }

        if (request.Phone is not null)
        {
            ThrowIfFailed(await userManager.SetPhoneNumberAsync(user, request.Phone));
        }

        return await MapOneAsync(user);
    }

    public async Task ChangePasswordAsync(
        string callerRole, Guid callerId, Guid userId,
        ChangePasswordRequest request, CancellationToken ct = default)
    {
        ApplicationUser user = await FindOrThrowAsync(userId);
        EnforceWriteAccess(user, callerRole, callerId);

        bool isSelfUpdate = user.Id == callerId;

        if (isSelfUpdate)
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            {
                throw new ArgumentException(ErrorMessages.User.CurrentPasswordRequired);
            }

            ThrowIfFailed(await userManager.ChangePasswordAsync(
                user, request.CurrentPassword, request.NewPassword));
        }
        else
        {
            ThrowIfFailed(await userManager.RemovePasswordAsync(user));
            ThrowIfFailed(await userManager.AddPasswordAsync(user, request.NewPassword));
        }

        user.MustChangePassword = false;
        ThrowIfFailed(await userManager.UpdateAsync(user));
    }

    /// <summary>
    /// Generates a standard Identity password reset token, later verified by
    /// ConfirmPasswordResetAsync, and flags the account so that
    /// MustChangePasswordMiddleware blocks all endpoints until the user
    /// completes the reset. Requires admin/owner privileges.
    /// </summary>
    public async Task<ResetPasswordResponse> ResetPasswordAsync(
        string callerRole, Guid callerId, Guid userId, CancellationToken ct = default)
    {
        ApplicationUser user = await FindOrThrowAsync(userId);
        EnforceResetPasswordAccess(user, callerRole, callerId);

        string token = await userManager.GeneratePasswordResetTokenAsync(user);

        user.MustChangePassword = true;
        ThrowIfFailed(await userManager.UpdateAsync(user));

        string frontendUrl = configuration[ConfigurationKeys.Frontend.PasswordResetUrl]
            ?? throw new InvalidOperationException(
                ErrorMessages.Configuration.KeyNotConfigured(ConfigurationKeys.Frontend.PasswordResetUrl));

        string resetLink =
            $"{frontendUrl}" +
            $"?email={Uri.EscapeDataString(user.Email!)}" +
            $"&token={Uri.EscapeDataString(token)}";

        await emailService.SendPasswordResetAsync(user.Email!, user.UserName!, resetLink, ct);

        return new ResetPasswordResponse(user.Id);
    }

    public async Task DeleteAsync(
        string callerRole, Guid callerId, Guid userId, CancellationToken ct = default)
    {
        ApplicationUser user = await FindOrThrowAsync(userId);
        EnforceDeleteAccess(user, callerRole, callerId);
        ThrowIfFailed(await userManager.DeleteAsync(user));
    }

    public async Task<UserResponse> SetActiveAsync(
        string callerRole, Guid callerId, Guid userId, bool isActive, CancellationToken ct = default)
    {
        ApplicationUser user = await FindOrThrowAsync(userId);
        EnforceDeleteAccess(user, callerRole, callerId);

        if (user.Id == callerId)
        {
            throw new InvalidOperationException(ErrorMessages.User.CannotChangeOwnActiveState);
        }

        if (isActive)
        {
            ThrowIfFailed(await userManager.SetLockoutEndDateAsync(user, null));
        }
        else
        {
            ThrowIfFailed(await userManager.SetLockoutEnabledAsync(user, true));
            ThrowIfFailed(await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue));
        }

        return await MapOneAsync(user);
    }

    private static void EnforceReadAccess(ApplicationUser target, string callerRole, Guid callerId)
    {
        if (IsAdmin(callerRole))
        {
            return;
        }

        if (IsOwner(callerRole) && (target.Id == callerId || target.CreatedByOwnerId == callerId))
        {
            return;
        }

        if (target.Id == callerId)
        {
            return;
        }

        throw new UnauthorizedAccessException(ErrorMessages.Auth.AccessDenied);
    }

    /// <summary>
    /// Write access currently follows the exact same rule as read access
    /// (Admin, the target's owner, or the target themselves). Kept as its
    /// own named method so a future divergence between the two policies
    /// doesn't require renaming call sites.
    /// </summary>
    private static void EnforceWriteAccess(ApplicationUser target, string callerRole, Guid callerId)
    {
        EnforceReadAccess(target, callerRole, callerId);
    }

    private static void EnforceResetPasswordAccess(ApplicationUser target, string callerRole, Guid callerId)
    {
        if (IsAdmin(callerRole))
        {
            return;
        }

        if (IsOwner(callerRole) && target.CreatedByOwnerId == callerId)
        {
            return;
        }

        throw new UnauthorizedAccessException(ErrorMessages.User.PasswordResetRestricted);
    }

    private static void EnforceDeleteAccess(ApplicationUser target, string callerRole, Guid callerId)
    {
        if (IsAdmin(callerRole))
        {
            return;
        }

        if (IsOwner(callerRole) && target.CreatedByOwnerId == callerId)
        {
            return;
        }

        throw new UnauthorizedAccessException(ErrorMessages.User.InsufficientPermissionsToDelete);
    }

    private async Task<UserResponse> MapOneAsync(ApplicationUser user)
    {
        IList<string> roles = await userManager.GetRolesAsync(user);
        string role = roles.FirstOrDefault() ?? string.Empty;
        bool isActive = !(user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow);
        return new UserResponse(user.Id, user.Email!, user.UserName!, role,
                                user.PhoneNumber, user.CreatedByOwnerId, isActive);
    }

    private async Task<IReadOnlyList<UserResponse>> MapManyAsync(IEnumerable<ApplicationUser> users)
    {
        List<UserResponse> results = [];
        foreach (ApplicationUser user in users)
        {
            results.Add(await MapOneAsync(user));
        }

        return results;
    }

    private async Task<ApplicationUser> FindOrThrowAsync(Guid userId)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId.ToString());
        return user ?? throw new KeyNotFoundException(ErrorMessages.User.NotFound(userId.ToString()));
    }

    private static void ThrowIfFailed(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    private IQueryable<ApplicationUser> ResolveVisibleUsersQuery(string callerRole, Guid callerId)
    {
        return IsAdmin(callerRole)
            ? userManager.Users
            : IsOwner(callerRole)
            ? userManager.Users.Where(u => u.CreatedByOwnerId == callerId)
            : throw new UnauthorizedAccessException(ErrorMessages.User.InsufficientPermissionsToListUsers);
    }

    private static bool IsAdmin(string role)
    {
        return role == Roles.Admin;
    }

    private static bool IsOwner(string role)
    {
        return role == Roles.Owner;
    }
}