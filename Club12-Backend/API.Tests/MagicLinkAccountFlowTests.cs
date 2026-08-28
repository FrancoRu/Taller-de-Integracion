using Application.DTOs.Auth.Request;
using Application.DTOs.Auth.Response;
using Application.Interfaces.Services;

using Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// HU-09 (invite by email + magic activation link) and HU-10 (self-service
/// password reset by magic link). The email sender is replaced with a capturing
/// fake so no real SMTP delivery happens: the tests assert the token
/// issue/consume + endpoint logic only. IdentityAuthenticationService is
/// constructed directly against a real UserManager/IAuthService resolved from
/// the host and an in-memory configuration carrying the frontend link URLs.
/// </summary>
public class MagicLinkAccountFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public MagicLinkAccountFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Capturing IEmailService test double — records the last link of each kind.</summary>
    private sealed class CapturingEmailService : IEmailService
    {
        public string? WelcomeLink { get; private set; }
        public string? ResetLink { get; private set; }
        public string? MagicLink { get; private set; }

        public Task SendWelcomeSetPasswordAsync(string toEmail, string toUsername, string setPasswordLink, CancellationToken ct = default)
        {
            WelcomeLink = setPasswordLink;
            return Task.CompletedTask;
        }

        public Task SendPasswordResetAsync(string toEmail, string toUsername, string resetLink, CancellationToken ct = default)
        {
            ResetLink = resetLink;
            return Task.CompletedTask;
        }

        public Task SendMagicLinkAsync(string toEmail, string toUsername, string magicLink, CancellationToken ct = default)
        {
            MagicLink = magicLink;
            return Task.CompletedTask;
        }
    }

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Frontend:ActivationUrl"] = "http://localhost:3001/auth/activate",
                ["Frontend:PasswordResetUrl"] = "http://localhost:3001/auth/password-reset",
            })
            .Build();

    private static (IdentityAuthenticationService service, UserManager<ApplicationUser> userManager, CapturingEmailService email)
        BuildService(IServiceScope scope)
    {
        UserManager<ApplicationUser> userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        IAuthService authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
        CapturingEmailService email = new();

        IdentityAuthenticationService service = new(userManager, authService, email, BuildConfig());
        return (service, userManager, email);
    }

    private static InviteUserRequest NewInvite() => new()
    {
        Email = $"invitee-{Guid.NewGuid():N}@club12.test",
        Role = "ADMIN",
    };

    // ---------------------------------------------------------------- HU-09

    [Fact]
    public async Task InviteUserAsync_CreatesUserWithoutPassword_AndIssuesActivationLink()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        (IdentityAuthenticationService service, UserManager<ApplicationUser> userManager, CapturingEmailService email) =
            BuildService(scope);

        InviteUserRequest request = NewInvite();

        InviteUserResponse response = await service.InviteUserAsync(request, "OWNER", Guid.NewGuid());

        Assert.NotEqual(Guid.Empty, response.UserId);
        Assert.Equal(request.Email, response.Email);

        ApplicationUser? created = await userManager.FindByEmailAsync(request.Email);
        Assert.NotNull(created);

        // The whole point of HU-09: no password is set at creation time.
        Assert.False(await userManager.HasPasswordAsync(created!));
        Assert.True(created!.MustChangePassword);

        // An activation link (carrying a real token) was emailed.
        Assert.NotNull(email.WelcomeLink);
        Assert.Contains("token=", email.WelcomeLink!);
        Assert.Contains("http://localhost:3001/auth/activate", email.WelcomeLink!);
    }

    [Fact]
    public async Task ActivateAccountAsync_SetsPasswordAndEnablesLogin()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        (IdentityAuthenticationService service, UserManager<ApplicationUser> userManager, _) = BuildService(scope);

        InviteUserRequest invite = NewInvite();
        await service.InviteUserAsync(invite, "OWNER", Guid.NewGuid());

        ApplicationUser user = (await userManager.FindByEmailAsync(invite.Email))!;
        string token = await userManager.GeneratePasswordResetTokenAsync(user);
        const string newPassword = "Activ8-Me!2026";

        TokenResponse tokens = await service.ActivateAccountAsync(new ActivateAccountRequest
        {
            Email = invite.Email,
            Token = token,
            NewPassword = newPassword,
        });

        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));

        ApplicationUser activated = (await userManager.FindByEmailAsync(invite.Email))!;
        Assert.True(await userManager.HasPasswordAsync(activated));
        Assert.False(activated.MustChangePassword);

        // Login now succeeds with the password the user just set.
        TokenResponse loginTokens = await service.LoginAsync(new Application.DTOs.Auth.Request.LogInUserRequest
        {
            Email = invite.Email,
            Password = newPassword,
        });
        Assert.False(string.IsNullOrWhiteSpace(loginTokens.AccessToken));
    }

    // ---------------------------------------------------------------- HU-10

    [Fact]
    public async Task RequestPasswordReset_ThenConfirm_ChangesThePassword()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        (IdentityAuthenticationService service, UserManager<ApplicationUser> userManager, CapturingEmailService email) =
            BuildService(scope);

        // Arrange: an active, password-holding account (invite + activate).
        InviteUserRequest invite = NewInvite();
        await service.InviteUserAsync(invite, "OWNER", Guid.NewGuid());
        ApplicationUser user = (await userManager.FindByEmailAsync(invite.Email))!;
        const string oldPassword = "Old-Password!2026";
        await service.ActivateAccountAsync(new ActivateAccountRequest
        {
            Email = invite.Email,
            Token = await userManager.GeneratePasswordResetTokenAsync(user),
            NewPassword = oldPassword,
        });

        // Act 1: self-service request emails a reset magic link.
        await service.RequestPasswordResetAsync(new RequestPasswordResetRequest { Email = invite.Email });
        Assert.NotNull(email.ResetLink);
        Assert.Contains("token=", email.ResetLink!);

        // Act 2: consume a reset token to set a brand-new password.
        const string newPassword = "New-Password!2026";
        await service.ConfirmPasswordResetAsync(new PasswordResetConfirmRequest
        {
            Email = invite.Email,
            Token = await userManager.GeneratePasswordResetTokenAsync(
                (await userManager.FindByEmailAsync(invite.Email))!),
            NewPassword = newPassword,
        });

        // Assert: new password works, old one no longer does.
        TokenResponse tokens = await service.LoginAsync(new Application.DTOs.Auth.Request.LogInUserRequest
        {
            Email = invite.Email,
            Password = newPassword,
        });
        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.LoginAsync(new Application.DTOs.Auth.Request.LogInUserRequest
            {
                Email = invite.Email,
                Password = oldPassword,
            }));
    }

    [Fact]
    public async Task RequestPasswordReset_UnknownEmail_DoesNotThrowAndSendsNothing()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        (IdentityAuthenticationService service, _, CapturingEmailService email) = BuildService(scope);

        await service.RequestPasswordResetAsync(new RequestPasswordResetRequest
        {
            Email = $"nobody-{Guid.NewGuid():N}@club12.test",
        });

        // No user enumeration: silent no-op, no email sent.
        Assert.Null(email.ResetLink);
    }
}
