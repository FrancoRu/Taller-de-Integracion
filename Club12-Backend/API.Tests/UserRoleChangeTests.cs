using Application.DTOs.User.Request;
using Application.DTOs.User.Response;
using Application.Interfaces.Services;

using Domain.Enums;

using Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// Verifies IUserManagementService.UpdateAsync's Role handling — the
/// capability that replaces raw "insert a row into AspNetUserRoles" access
/// with a real, authorized role-change path. Covers the two callers allowed
/// to change roles at all (ADMIN, OWNER), the privilege-escalation guards
/// (a caller can never grant a role beyond its own assignment policy, and
/// nobody — not even ADMIN — can change their own role), and the Identity
/// invariant that a user ends up in exactly one role after the change.
/// </summary>
public class UserRoleChangeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public UserRoleChangeTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UpdateAsync_AdminChangesAnyUsersRole_Succeeds()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        UserManager<ApplicationUser> userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        IUserManagementService userManagementService =
            scope.ServiceProvider.GetRequiredService<IUserManagementService>();

        (Guid adminId, _) = await SeedUserWithRoleAsync(userManager, UserRoleType.ADMIN);
        (Guid targetId, _) = await SeedUserWithRoleAsync(userManager, UserRoleType.TOURNAMENT_MANAGER);

        UserResponse result = await userManagementService.UpdateAsync(
            Roles.Admin, adminId, targetId,
            new UpdateUserRequest { Role = UserRoleType.TEAM_MANAGER });

        Assert.Equal(Roles.TeamManager, result.Role);
    }

    [Fact]
    public async Task UpdateAsync_OwnerChangesOwnSubordinatesRole_Succeeds()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        UserManager<ApplicationUser> userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        IUserManagementService userManagementService =
            scope.ServiceProvider.GetRequiredService<IUserManagementService>();

        (Guid ownerId, _) = await SeedUserWithRoleAsync(userManager, UserRoleType.OWNER);
        (Guid subordinateId, ApplicationUser subordinate) =
            await SeedUserWithRoleAsync(userManager, UserRoleType.TOURNAMENT_MANAGER);
        subordinate.CreatedByOwnerId = ownerId;
        await userManager.UpdateAsync(subordinate);

        UserResponse result = await userManagementService.UpdateAsync(
            Roles.Owner, ownerId, subordinateId,
            new UpdateUserRequest { Role = UserRoleType.TOURNAMENT_MANAGER });

        Assert.Equal(Roles.TournamentManager, result.Role);
    }

    [Fact]
    public async Task UpdateAsync_NonAdminNonOwnerCaller_IsRejected()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        UserManager<ApplicationUser> userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        IUserManagementService userManagementService =
            scope.ServiceProvider.GetRequiredService<IUserManagementService>();

        (Guid callerId, _) = await SeedUserWithRoleAsync(userManager, UserRoleType.TOURNAMENT_MANAGER);
        (Guid targetId, _) = await SeedUserWithRoleAsync(userManager, UserRoleType.TEAM_MANAGER);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            userManagementService.UpdateAsync(
                Roles.TournamentManager, callerId, targetId,
                new UpdateUserRequest { Role = UserRoleType.OWNER }));
    }

    [Fact]
    public async Task UpdateAsync_OwnerTargetingUserTheyDidNotCreate_IsRejected()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        UserManager<ApplicationUser> userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        IUserManagementService userManagementService =
            scope.ServiceProvider.GetRequiredService<IUserManagementService>();

        (Guid ownerId, _) = await SeedUserWithRoleAsync(userManager, UserRoleType.OWNER);
        // Not created by this owner (CreatedByOwnerId left null).
        (Guid targetId, _) = await SeedUserWithRoleAsync(userManager, UserRoleType.TOURNAMENT_MANAGER);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            userManagementService.UpdateAsync(
                Roles.Owner, ownerId, targetId,
                new UpdateUserRequest { Role = UserRoleType.TOURNAMENT_MANAGER }));
    }

    [Fact]
    public async Task UpdateAsync_OwnerAssigningRoleOutsideItsPolicy_IsRejected()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        UserManager<ApplicationUser> userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        IUserManagementService userManagementService =
            scope.ServiceProvider.GetRequiredService<IUserManagementService>();

        (Guid ownerId, _) = await SeedUserWithRoleAsync(userManager, UserRoleType.OWNER);
        (Guid subordinateId, ApplicationUser subordinate) =
            await SeedUserWithRoleAsync(userManager, UserRoleType.TOURNAMENT_MANAGER);
        subordinate.CreatedByOwnerId = ownerId;
        await userManager.UpdateAsync(subordinate);

        // OWNER may only assign TOURNAMENT_MANAGER — never OWNER or ADMIN.
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            userManagementService.UpdateAsync(
                Roles.Owner, ownerId, subordinateId,
                new UpdateUserRequest { Role = UserRoleType.OWNER }));
    }

    [Fact]
    public async Task UpdateAsync_InvalidRoleValue_IsRejected()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        UserManager<ApplicationUser> userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        IUserManagementService userManagementService =
            scope.ServiceProvider.GetRequiredService<IUserManagementService>();

        (Guid adminId, _) = await SeedUserWithRoleAsync(userManager, UserRoleType.ADMIN);
        (Guid targetId, _) = await SeedUserWithRoleAsync(userManager, UserRoleType.TEAM_MANAGER);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            userManagementService.UpdateAsync(
                Roles.Admin, adminId, targetId,
                new UpdateUserRequest { Role = (UserRoleType)999 }));
    }

    /// <summary>
    /// Closes the self-escalation vector this task exists to fix: even the
    /// most privileged caller (ADMIN) cannot use their own privilege to grant
    /// themselves a higher role while editing their own profile.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_CallerChangingOwnRole_IsRejectedEvenForAdmin()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        UserManager<ApplicationUser> userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        IUserManagementService userManagementService =
            scope.ServiceProvider.GetRequiredService<IUserManagementService>();

        (Guid adminId, _) = await SeedUserWithRoleAsync(userManager, UserRoleType.ADMIN);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            userManagementService.UpdateAsync(
                Roles.Admin, adminId, adminId,
                new UpdateUserRequest { Role = UserRoleType.ADMIN }));
    }

    [Fact]
    public async Task UpdateAsync_RoleChange_ReplacesOldRoleSoUserEndsUpInExactlyOneRole()
    {
        Guid adminId;
        Guid targetId;
        using (IServiceScope seedScope = _factory.Services.CreateScope())
        {
            UserManager<ApplicationUser> seedUserManager =
                seedScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            (adminId, _) = await SeedUserWithRoleAsync(seedUserManager, UserRoleType.ADMIN);
            (targetId, _) = await SeedUserWithRoleAsync(seedUserManager, UserRoleType.TOURNAMENT_MANAGER);
        }

        using (IServiceScope actScope = _factory.Services.CreateScope())
        {
            IUserManagementService userManagementService =
                actScope.ServiceProvider.GetRequiredService<IUserManagementService>();

            await userManagementService.UpdateAsync(
                Roles.Admin, adminId, targetId,
                new UpdateUserRequest { Role = UserRoleType.TEAM_MANAGER });
        }

        using IServiceScope verifyScope = _factory.Services.CreateScope();
        UserManager<ApplicationUser> verifyUserManager =
            verifyScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        ApplicationUser? persistedTarget = await verifyUserManager.FindByIdAsync(targetId.ToString());
        Assert.NotNull(persistedTarget);

        IList<string> rolesAfterChange = await verifyUserManager.GetRolesAsync(persistedTarget!);

        Assert.Single(rolesAfterChange);
        Assert.Equal(Roles.TeamManager, rolesAfterChange[0]);
    }

    [Fact]
    public async Task UpdateAsync_ProfileFieldsOnly_DoesNotTouchRole()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        UserManager<ApplicationUser> userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        IUserManagementService userManagementService =
            scope.ServiceProvider.GetRequiredService<IUserManagementService>();

        (Guid adminId, _) = await SeedUserWithRoleAsync(userManager, UserRoleType.ADMIN);
        (Guid targetId, _) = await SeedUserWithRoleAsync(userManager, UserRoleType.TOURNAMENT_MANAGER);

        UserResponse result = await userManagementService.UpdateAsync(
            Roles.Admin, adminId, targetId,
            new UpdateUserRequest { Username = $"renamed-{Guid.NewGuid():N}" });

        Assert.Equal(Roles.TournamentManager, result.Role);
    }

    private static async Task<(Guid Id, ApplicationUser User)> SeedUserWithRoleAsync(
        UserManager<ApplicationUser> userManager, UserRoleType role)
    {
        string uniqueEmail = $"role-change-test-{Guid.NewGuid()}@test.local";

        ApplicationUser user = new()
        {
            UserName = uniqueEmail,
            Email = uniqueEmail,
            EmailConfirmed = true,
        };

        IdentityResult createResult = await userManager.CreateAsync(user, "Test-Passw0rd!1");
        Assert.True(createResult.Succeeded, string.Join(" | ", createResult.Errors.Select(e => e.Description)));

        IdentityResult roleResult = await userManager.AddToRoleAsync(user, role.ToRoleName());
        Assert.True(roleResult.Succeeded, string.Join(" | ", roleResult.Errors.Select(e => e.Description)));

        return (user.Id, user);
    }
}
