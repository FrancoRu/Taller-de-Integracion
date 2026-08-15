using System;
using System.Threading.Tasks;

namespace API.Tests.Backup.Fakes;

/// <summary>
/// Polls a condition instead of sleeping for a fixed real-time duration, so
/// timing-sensitive hosted-service tests stay fast on quick machines and
/// resilient (non-flaky) on slow/loaded CI runners.
/// </summary>
internal static class TestTiming
{
    public static async Task<bool> WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return true;
            await Task.Delay(10);
        }
        return condition();
    }
}
