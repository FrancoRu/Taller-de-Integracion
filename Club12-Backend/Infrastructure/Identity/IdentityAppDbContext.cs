using Domain.Enums;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using System;

namespace Infrastructure.Identity;

/// <summary>
/// EF Core context for ASP.NET Core Identity tables.
/// Inherits IdentityDbContext{TUser,TRole,TKey} so EF generates
/// AspNetUsers, AspNetRoles, AspNetUserRoles, etc.
/// </summary>
public class IdentityAppDbContext(DbContextOptions<IdentityAppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>()
            .HasIndex(u => u.NormalizedEmail)
            .IsUnique();

        SeedRoles(builder);
    }

    private static void SeedRoles(ModelBuilder builder)
    {
        builder.Entity<IdentityRole<Guid>>().HasData(
            CreateRole(1, UserRoleType.ADMIN),
            CreateRole(2, UserRoleType.OWNER),
            CreateRole(3, UserRoleType.TOURNAMENT_MANAGER),
            CreateRole(4, UserRoleType.TEAM_MANAGER),
            CreateRole(5, UserRoleType.GUEST)
        );
    }

    private static IdentityRole<Guid> CreateRole(int seed, UserRoleType roleType)
    {
        string name = roleType.ToRoleName();
        return new IdentityRole<Guid>
        {
            Id = new Guid($"00000000-0000-0000-0000-{seed:D12}"),
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            ConcurrencyStamp = seed.ToString()
        };
    }
}