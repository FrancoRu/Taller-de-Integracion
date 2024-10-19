using Entities.Models.PlayerSanctionEntity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services.Services.PlayerSanctionService;
using Services.Services.PlayerService;

namespace Services.BackgroundServices;

/// <summary>
/// A hosted service for cleaning up expired player sanctions.
/// </summary>
public class SanctionCleanupService(
    IPlayerSanctionService playerSanctionGenericService,
    IPlayerService playerGenericService,
    ILogger<SanctionCleanupService> logger) : IHostedService, IDisposable
{
    private const int TIMERINTERVALINHOURS = 24;
    private const string LOGEXPIREDSANCTIONSMESSAGE = "{Count} expired sanctions have been cleaned up.";
    private const string LOGNOEXPIREDSANCTIONSMESSAGE = "No expired sanctions found.";
    private const string LOGERRORMESSAGE = "An error occurred while cleaning up expired sanctions: {ExceptionMessage}";
    private Timer? _timer;

    /// <summary>
    /// Starts the hosted service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for stopping the service.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(CleanupSanctions, null, TimeSpan.Zero, TimeSpan.FromHours(TIMERINTERVALINHOURS));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Cleans up expired player sanctions and updates player statuses.
    /// </summary>
    /// <param name="state">The state object passed to the timer callback.</param>
    private async void CleanupSanctions(object? state)
    {
        try
        {
            DateTime today = DateTime.UtcNow;
            IEnumerable<PlayerSanction>? expiredSanctions = await playerSanctionGenericService.GetExpiredSanctionsAsync(today);

            if (expiredSanctions is not null && expiredSanctions.Any())
            {
                await Task.WhenAll(expiredSanctions.Select(async sanction =>
                {
                    await playerSanctionGenericService.DeletePlayerSanctionAsync(sanction);

                    if (sanction.Player is not null)
                    {
                        sanction.Player.IsSanctioned = false;
                        await playerGenericService.UpdatePlayer(sanction.Player);
                    }
                }));

                logger.LogInformation(LOGEXPIREDSANCTIONSMESSAGE, expiredSanctions.Count());
            }
            else
            {
                logger.LogInformation(LOGNOEXPIREDSANCTIONSMESSAGE);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(LOGERRORMESSAGE, ex.Message);
        }
    }

    /// <summary>
    /// Stops the hosted service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for stopping the service.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Releases the resources used by the hosted service.
    /// </summary>
    public void Dispose()
    {
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
