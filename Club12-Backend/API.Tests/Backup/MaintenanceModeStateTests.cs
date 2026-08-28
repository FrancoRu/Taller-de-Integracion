using Application.Backup;
using Application.Interfaces.Backup;

namespace API.Tests.Backup;

/// <summary>
/// Unit tests for MaintenanceModeState — pure in-memory process
/// state (design.md: "Maintenance state lives in Application/Backup, not
/// Infrastructure", registered as a singleton). Covers the
/// Enter/Exit transitions and the IsActive/Reason/EnteredAtUtc
/// shape MaintenanceStatusResponse projects.
/// </summary>
public sealed class MaintenanceModeStateTests
{
    [Fact]
    public void InitialState_IsNotActive_ReasonAndEnteredAtUtcAreNull()
    {
        IMaintenanceModeState state = new MaintenanceModeState();

        Assert.False(state.IsActive);
        Assert.Null(state.Reason);
        Assert.Null(state.EnteredAtUtc);
    }

    [Fact]
    public void Enter_SetsIsActiveTrue_AndReason_AndEnteredAtUtc()
    {
        IMaintenanceModeState state = new MaintenanceModeState();
        DateTimeOffset before = DateTimeOffset.UtcNow;

        state.Enter("Restoring backup abc123");

        DateTimeOffset after = DateTimeOffset.UtcNow;
        Assert.True(state.IsActive);
        Assert.Equal("Restoring backup abc123", state.Reason);
        Assert.NotNull(state.EnteredAtUtc);
        Assert.InRange(state.EnteredAtUtc!.Value, before, after);
    }

    [Fact]
    public void Exit_AfterEnter_ClearsIsActive_ReasonAndEnteredAtUtc()
    {
        IMaintenanceModeState state = new MaintenanceModeState();
        state.Enter("Restoring backup abc123");

        state.Exit();

        Assert.False(state.IsActive);
        Assert.Null(state.Reason);
        Assert.Null(state.EnteredAtUtc);
    }

    /// <summary>
    /// The manual escape hatch (DELETE api/maintenance) must be able
    /// to clear a stuck window even when no restore is in flight — Exit()
    /// has no precondition on a prior Enter().
    /// </summary>
    [Fact]
    public void Exit_WithoutPriorEnter_DoesNotThrow_StaysInactive()
    {
        IMaintenanceModeState state = new MaintenanceModeState();

        state.Exit();

        Assert.False(state.IsActive);
    }

    [Fact]
    public void Enter_TwiceWithDifferentReasons_LatestReasonWins()
    {
        IMaintenanceModeState state = new MaintenanceModeState();

        state.Enter("First reason");
        state.Enter("Second reason");

        Assert.True(state.IsActive);
        Assert.Equal("Second reason", state.Reason);
    }
}
