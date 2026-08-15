using Application.Interfaces.Services;
using FluentEmail.Core;
using FluentEmail.Core.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Utils.Helper.Email;

/// <summary>
/// Transactional email sender backed by FluentEmail + SendGrid.
/// HTML bodies are loaded from embedded resource templates in
/// Utils/Helper/Email/Templates/.
/// </summary>
public sealed class FluentEmailHelper(IFluentEmailFactory emailFactory) : IEmailService
{
    // ─────────────────────────────────────────────────────────────
    // Password reset
    // ─────────────────────────────────────────────────────────────

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

    // ─────────────────────────────────────────────────────────────
    // Welcome + set password (newly created users)
    // ─────────────────────────────────────────────────────────────

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

    // ─────────────────────────────────────────────────────────────
    // Magic link (TeamManager)
    // ─────────────────────────────────────────────────────────────

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

    // ─────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────

    private static void ThrowIfFailed(SendResponse result, string context)
    {
        if (!result.Successful)
            throw new InvalidOperationException(
                $"Failed to send {context} email: {string.Join(", ", result.ErrorMessages)}");
    }
}
