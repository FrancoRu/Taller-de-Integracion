using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

public interface IEmailService
{
    Task SendPasswordResetAsync(
        string toEmail, string toUsername, string resetLink,
        CancellationToken ct = default);

    Task SendMagicLinkAsync(
        string toEmail, string toUsername, string magicLink,
        CancellationToken ct = default);

    Task SendWelcomeSetPasswordAsync(
        string toEmail, string toUsername, string setPasswordLink,
        CancellationToken ct = default);
}