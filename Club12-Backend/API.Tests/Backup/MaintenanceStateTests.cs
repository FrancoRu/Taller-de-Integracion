using System.Diagnostics.CodeAnalysis;

using Application.Interfaces.Maintenance;
using Application.Maintenance;

namespace API.Tests.Backup;

/// <summary>
/// Unit tests for the HU-92 maintenance lock: entering sets the active state,
/// a second entry while held is rejected, and disposing the lease restores
/// the idle state so the next operation can acquire it.
/// </summary>
public class MaintenanceStateTests
{
    [Fact]
    public void Enter_WhenIdle_BecomesActiveWithStatus()
    {
        MaintenanceState state = new();

        Assert.False(state.IsActive);
        Assert.Null(state.Current);

        using IDisposable lease = state.Enter("backup");

        Assert.True(state.IsActive);
        Assert.NotNull(state.Current);
        Assert.Equal("backup", state.Current!.Operation);
    }

    [Fact]
    public void Enter_WhileAlreadyHeld_ThrowsWithRunningStatus()
    {
        MaintenanceState state = new();
        using IDisposable lease = state.Enter("restore");

        MaintenanceInProgressException ex =
            Assert.Throws<MaintenanceInProgressException>(() => state.Enter("backup"));

        Assert.Equal("restore", ex.Status!.Operation);
        Assert.True(state.IsActive, "The original lease must remain active after a rejected second entry.");
    }

    [Fact]
    public void Dispose_ReleasesLock_AllowingReacquire()
    {
        MaintenanceState state = new();

        IDisposable lease = state.Enter("backup");
        lease.Dispose();

        Assert.False(state.IsActive);
        Assert.Null(state.Current);

        // Unlock restores the ability to acquire again.
        using IDisposable second = state.Enter("restore");
        Assert.True(state.IsActive);
        Assert.Equal("restore", state.Current!.Operation);
    }

    [Fact]
    [SuppressMessage(
        "SonarAnalyzer",
        "S3966:Objects should not be disposed more than once",
        Justification = "Disposing a stale lease a second time is exactly the behavior under test: it must be a safe no-op and must not release the newer, still-active lease.")]
    public void Dispose_IsIdempotent_DoesNotReleaseANewerLease()
    {
        MaintenanceState state = new();

        IDisposable first = state.Enter("backup");
        first.Dispose();

        using IDisposable second = state.Enter("restore");
        first.Dispose(); // disposing the stale lease must NOT release the active one

        Assert.True(state.IsActive);
        Assert.Equal("restore", state.Current!.Operation);
    }
}
