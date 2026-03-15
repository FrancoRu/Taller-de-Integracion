using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Identity;

/// <summary>
/// Seeds the initial admin user into the Identity database at startup.
/// Credentials are read from configuration (AdminUser:Email / AdminUser:Password).
/// Runs only once — skips silently if the user already exists.
/// </summary>
public sealed class IdentitySeeder(
    UserManager<ApplicationUser> userManager,
    IConfiguration               configuration,
    ILogger<IdentitySeeder>      logger)
{
    public async Task SeedAsync()
    {
        await SeedAdminAsync();
    }

    private async Task SeedAdminAsync()
    {
        string? email    = configuration["AdminUser:Email"];
        string? password = configuration["AdminUser:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "AdminUser:Email or AdminUser:Password not configured — skipping admin seeding.");
            return;
        }

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            logger.LogInformation("Admin user '{Email}' already exists — skipping.", email);
            return;
        }

        ApplicationUser admin = new()
        {
            UserName       = email,
            Email          = email,
            EmailConfirmed = true,
        };

        IdentityResult result = await userManager.CreateAsync(admin, password);

        if (!result.Succeeded)
        {
            string errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogError("Failed to create admin user '{Email}': {Errors}", email, errors);
            return;
        }

        await userManager.AddToRoleAsync(admin, UserRoleType.ADMIN.ToRoleName());

        logger.LogInformation("Admin user '{Email}' created and assigned role ADMIN.", email);
    }
}