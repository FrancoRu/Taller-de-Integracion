using Application.Interfaces.Services;

using FluentEmail.Core;
using FluentEmail.Core.Models;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Email;

/// <summary>
/// Transactional email sender backed by FluentEmail and SendGrid.
/// </summary>
public sealed class FluentEmailHelper(IFluentEmailFactory emailFactory) : IEmailService
{
    public async Task SendPasswordResetAsync(
        string toEmail, string toUsername, string resetLink,
        CancellationToken ct = default)
    {
        string body = EmailTemplateLoader.Render("PasswordResetTemplate", new()
        {
            ["{{Username}}"] = toUsername,
            ["{{ResetLink}}"] = resetLink,
        });

        SendResponse result = await emailFactory.Create()
            .To(toEmail)
            .Subject("Restablecimiento de contraseña - Club12")
            .Body(body, isHtml: true)
            .SendAsync(ct);

        ThrowIfFailed(result, "password reset");
    }

    public async Task SendWelcomeSetPasswordAsync(
        string toEmail, string toUsername, string setPasswordLink,
        CancellationToken ct = default)
    {
        string body = EmailTemplateLoader.Render("WelcomeSetPasswordTemplate", new()
        {
            ["{{Username}}"] = toUsername,
            ["{{SetPasswordLink}}"] = setPasswordLink,
        });

        SendResponse result = await emailFactory.Create()
            .To(toEmail)
            .Subject("Activá tu cuenta - Club12")
            .Body(body, isHtml: true)
            .SendAsync(ct);

        ThrowIfFailed(result, "welcome set-password");
    }

    public async Task SendMagicLinkAsync(
        string toEmail, string toUsername, string magicLink,
        CancellationToken ct = default)
    {
        string body = EmailTemplateLoader.Render("MagicLinkTemplate", new()
        {
            ["{{Username}}"] = toUsername,
            ["{{MagicLink}}"] = magicLink,
        });

        SendResponse result = await emailFactory.Create()
            .To(toEmail)
            .Subject("Tu enlace de acceso - Club12")
            .Body(body, isHtml: true)
            .SendAsync(ct);

        ThrowIfFailed(result, "magic link");
    }

    private static void ThrowIfFailed(SendResponse result, string context)
    {
        if (!result.Successful)
        {
            throw new InvalidOperationException(
                $"Failed to send {context} email: {string.Join(", ", result.ErrorMessages)}");
        }
    }
}
