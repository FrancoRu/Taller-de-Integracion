using Application.DTOs.Abstract.Request;
using Application.DTOs.Abstract.Response;
using Application.DTOs.User.Request;
using Application.DTOs.User.Response;
using Application.Interfaces.Services;
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
/// Identity-backed implementation of <see cref="IUserManagementService"/>.
/// All access-control rules are enforced here, keeping controllers thin.
/// </summary>
public sealed class IdentityUserManagementService(
    UserManager<ApplicationUser> userManager,
    IdentityAppDbContext          identityDbContext,
    IEmailService                emailService,
    IConfiguration               configuration) : IUserManagementService
{
    // ─────────────────────────────────────────────────────────────
    // GetAll  (paginated + filtered)
    // ─────────────────────────────────────────────────────────────

    public async Task<PaginatedResponse<UserResponse>> GetAllAsync(
        string callerRole, Guid callerId,
        UserFilteredRequest filter,
        CancellationToken ct)
    {
        // 1. Base scope.
        IQueryable<ApplicationUser> query = IsAdmin(callerRole)
            ? userManager.Users
            : IsOwner(callerRole)
                ? userManager.Users.Where(u => u.CreatedByOwnerId == callerId)
                : throw new UnauthorizedAccessException("Insufficient permissions to list users.");

        // 2. Text filters.
        if (!string.IsNullOrWhiteSpace(filter.UserName))
            query = query.Where(u => u.UserName != null &&
                                     u.UserName.ToLower().Contains(filter.UserName.ToLower()));

        if (!string.IsNullOrWhiteSpace(filter.Email))
            query = query.Where(u => u.Email != null &&
                                     u.Email.ToLower().Contains(filter.Email.ToLower()));

        if (!string.IsNullOrWhiteSpace(filter.PhoneNumber))
            query = query.Where(u => u.PhoneNumber != null &&
                                     u.PhoneNumber.Contains(filter.PhoneNumber));

        // 3. Role filter via UserRoles join.
        if (filter.Role.HasValue)
        {
            string roleName = filter.Role.Value.ToRoleName();

            IQueryable<Guid> userIdsWithRole =
                from ur in identityDbContext.UserRoles
                join r  in identityDbContext.Roles on ur.RoleId equals r.Id
                where r.Name == roleName
                select ur.UserId;

            query = query.Where(u => userIdsWithRole.Contains(u.Id));
        }

        // 4. Total count before pagination.
        int totalCount = await query.CountAsync(ct);

        // 5. Ordering.
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

        // 6. Pagination.
        int skip = (filter.PageNumber - 1) * filter.PageSize;
        List<ApplicationUser> users = await query.Skip(skip).Take(filter.PageSize).ToListAsync(ct);

        // 7. Map.
        IReadOnlyList<UserResponse> items = await MapManyAsync(users);

        return new PaginatedResponse<UserResponse>
        {
            Items      = items,
            Page       = filter.PageNumber,
            PageSize   = filter.PageSize,
            TotalCount = totalCount,
        };
    }

    // ─────────────────────────────────────────────────────────────
    // GetById
    // ─────────────────────────────────────────────────────────────

    public async Task<UserResponse> GetByIdAsync(
        string callerRole, Guid callerId, Guid userId, CancellationToken ct)
    {
        ApplicationUser user = await FindOrThrowAsync(userId);
        EnforceReadAccess(user, callerRole, callerId);
        return await MapOneAsync(user);
    }

    // ─────────────────────────────────────────────────────────────
    // Update  (profile only — no password)
    // ─────────────────────────────────────────────────────────────

    public async Task<UserResponse> UpdateAsync(
        string callerRole, Guid callerId, Guid userId,
        UpdateUserRequest request, CancellationToken ct)
    {
        ApplicationUser user = await FindOrThrowAsync(userId);
        EnforceWriteAccess(user, callerRole, callerId);

        if (!string.IsNullOrWhiteSpace(request.Username))
            ThrowIfFailed(await userManager.SetUserNameAsync(user, request.Username));

        if (!string.IsNullOrWhiteSpace(request.Email))
            ThrowIfFailed(await userManager.SetEmailAsync(user, request.Email));

        if (request.Phone is not null)
            ThrowIfFailed(await userManager.SetPhoneNumberAsync(user, request.Phone));

        return await MapOneAsync(user);
    }

    // ─────────────────────────────────────────────────────────────
    // ChangePassword  (self-service or privileged)
    // ─────────────────────────────────────────────────────────────

    public async Task ChangePasswordAsync(
        string callerRole, Guid callerId, Guid userId,
        ChangePasswordRequest request, CancellationToken ct)
    {
        ApplicationUser user = await FindOrThrowAsync(userId);
        EnforceWriteAccess(user, callerRole, callerId);

        bool isSelfUpdate = user.Id == callerId;

        if (isSelfUpdate)
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                throw new ArgumentException("CurrentPassword is required when changing your own password.");

            ThrowIfFailed(await userManager.ChangePasswordAsync(
                user, request.CurrentPassword, request.NewPassword));
        }
        else
        {
            ThrowIfFailed(await userManager.RemovePasswordAsync(user));
            ThrowIfFailed(await userManager.AddPasswordAsync(user, request.NewPassword));
        }

        user.MustChangePassword = false;                       // ← nuevo
        ThrowIfFailed(await userManager.UpdateAsync(user));    // ← nuevo
    }

    // ─────────────────────────────────────────────────────────────
    // ResetPassword  (admin/owner → sends email with reset link)
    // ─────────────────────────────────────────────────────────────

    public async Task<ResetPasswordResponse> ResetPasswordAsync(
        string callerRole, Guid callerId, Guid userId, CancellationToken ct)
    {
        ApplicationUser user = await FindOrThrowAsync(userId);
        EnforceResetPasswordAccess(user, callerRole, callerId);

        // Standard Identity reset token (verified later by ConfirmPasswordResetAsync).
        string token = await userManager.GeneratePasswordResetTokenAsync(user);

        // Flag the account — MustChangePasswordMiddleware blocks all endpoints until resolved.
        user.MustChangePassword = true;
        ThrowIfFailed(await userManager.UpdateAsync(user));

        // Build the frontend link; token MUST be URL-encoded.
        string frontendUrl = configuration["Frontend:PasswordResetUrl"]
            ?? throw new InvalidOperationException("Frontend:PasswordResetUrl is not configured.");

        string resetLink =
            $"{frontendUrl}" +
            $"?email={Uri.EscapeDataString(user.Email!)}" +
            $"&token={Uri.EscapeDataString(token)}";

        await emailService.SendPasswordResetAsync(user.Email!, user.UserName!, resetLink, ct);

        return new ResetPasswordResponse(user.Id);
    }

    // ─────────────────────────────────────────────────────────────
    // Delete
    // ─────────────────────────────────────────────────────────────

    public async Task DeleteAsync(
        string callerRole, Guid callerId, Guid userId, CancellationToken ct)
    {
        ApplicationUser user = await FindOrThrowAsync(userId);
        EnforceDeleteAccess(user, callerRole, callerId);
        ThrowIfFailed(await userManager.DeleteAsync(user));
    }

    // ─────────────────────────────────────────────────────────────
    // Access-control helpers
    // ─────────────────────────────────────────────────────────────

    private static void EnforceReadAccess(ApplicationUser target, string callerRole, Guid callerId)
    {
        if (IsAdmin(callerRole)) return;
        if (IsOwner(callerRole) && (target.Id == callerId || target.CreatedByOwnerId == callerId)) return;
        if (target.Id == callerId) return;
        throw new UnauthorizedAccessException("Access denied.");
    }

    private static void EnforceWriteAccess(ApplicationUser target, string callerRole, Guid callerId)
    {
        if (IsAdmin(callerRole)) return;
        if (IsOwner(callerRole) && (target.Id == callerId || target.CreatedByOwnerId == callerId)) return;
        if (target.Id == callerId) return;
        throw new UnauthorizedAccessException("Access denied.");
    }

    private static void EnforceResetPasswordAccess(ApplicationUser target, string callerRole, Guid callerId)
    {
        if (IsAdmin(callerRole)) return;
        if (IsOwner(callerRole) && target.CreatedByOwnerId == callerId) return;
        throw new UnauthorizedAccessException(
            "Only Admins and Owners (for their own subordinates) can reset passwords.");
    }

    private static void EnforceDeleteAccess(ApplicationUser target, string callerRole, Guid callerId)
    {
        if (IsAdmin(callerRole)) return;
        if (IsOwner(callerRole) && target.CreatedByOwnerId == callerId) return;
        throw new UnauthorizedAccessException("Insufficient permissions to delete this user.");
    }

    // ─────────────────────────────────────────────────────────────
    // Mapping helpers
    // ─────────────────────────────────────────────────────────────

    private async Task<UserResponse> MapOneAsync(ApplicationUser user)
    {
        IList<string> roles = await userManager.GetRolesAsync(user);
        string role = roles.FirstOrDefault() ?? string.Empty;
        return new UserResponse(user.Id, user.Email!, user.UserName!, role,
                                user.PhoneNumber, user.CreatedByOwnerId);
    }

    private async Task<IReadOnlyList<UserResponse>> MapManyAsync(IEnumerable<ApplicationUser> users)
    {
        var results = new List<UserResponse>();
        foreach (ApplicationUser user in users)
            results.Add(await MapOneAsync(user));
        return results;
    }

    // ─────────────────────────────────────────────────────────────
    // Find / throw helpers
    // ─────────────────────────────────────────────────────────────

    private async Task<ApplicationUser> FindOrThrowAsync(Guid userId)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId.ToString());
        return user ?? throw new KeyNotFoundException($"User '{userId}' not found.");
    }

    private static void ThrowIfFailed(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(
                string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    private static bool IsAdmin(string role) => role == Roles.Admin;
    private static bool IsOwner(string role) => role == Roles.Owner;
}