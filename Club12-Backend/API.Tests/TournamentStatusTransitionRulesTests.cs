using Domain.Enums;

namespace API.Tests;

/// <summary>
/// Pure state-machine tests for <see cref="TournamentStatusTransitions"/>
/// (HU-35): the forward-only lifecycle
/// Scheduled -> OpenForRegistration -> RegistrationClosed -> Ongoing ->
/// Finished, with Canceled reachable from any non-terminal state and
/// Finished/Canceled terminal.
/// </summary>
public class TournamentStatusTransitionRulesTests
{
    [Theory]
    [InlineData(TournamentStatus.Scheduled, TournamentStatus.OpenForRegistration)]
    [InlineData(TournamentStatus.OpenForRegistration, TournamentStatus.RegistrationClosed)]
    [InlineData(TournamentStatus.RegistrationClosed, TournamentStatus.Ongoing)]
    [InlineData(TournamentStatus.Ongoing, TournamentStatus.Finished)]
    public void IsValidTransition_ForwardHappyPath_IsAllowed(TournamentStatus from, TournamentStatus to)
    {
        Assert.True(TournamentStatusTransitions.IsValidTransition(from, to));
    }

    [Theory]
    [InlineData(TournamentStatus.Scheduled)]
    [InlineData(TournamentStatus.OpenForRegistration)]
    [InlineData(TournamentStatus.RegistrationClosed)]
    [InlineData(TournamentStatus.Ongoing)]
    public void IsValidTransition_CancelFromAnyNonTerminal_IsAllowed(TournamentStatus from)
    {
        Assert.True(TournamentStatusTransitions.IsValidTransition(from, TournamentStatus.Canceled));
    }

    [Theory]
    // Skipping a step forward.
    [InlineData(TournamentStatus.Scheduled, TournamentStatus.RegistrationClosed)]
    [InlineData(TournamentStatus.Scheduled, TournamentStatus.Ongoing)]
    [InlineData(TournamentStatus.OpenForRegistration, TournamentStatus.Ongoing)]
    [InlineData(TournamentStatus.RegistrationClosed, TournamentStatus.Finished)]
    // Going backward.
    [InlineData(TournamentStatus.OpenForRegistration, TournamentStatus.Scheduled)]
    [InlineData(TournamentStatus.RegistrationClosed, TournamentStatus.OpenForRegistration)]
    [InlineData(TournamentStatus.Ongoing, TournamentStatus.RegistrationClosed)]
    [InlineData(TournamentStatus.Finished, TournamentStatus.Ongoing)]
    public void IsValidTransition_SkippingOrBackward_IsRejected(TournamentStatus from, TournamentStatus to)
    {
        Assert.False(TournamentStatusTransitions.IsValidTransition(from, to));
    }

    [Theory]
    [InlineData(TournamentStatus.Finished, TournamentStatus.OpenForRegistration)]
    [InlineData(TournamentStatus.Finished, TournamentStatus.Canceled)]
    [InlineData(TournamentStatus.Canceled, TournamentStatus.Ongoing)]
    [InlineData(TournamentStatus.Canceled, TournamentStatus.OpenForRegistration)]
    public void IsValidTransition_OutOfTerminalState_IsRejected(TournamentStatus from, TournamentStatus to)
    {
        Assert.False(TournamentStatusTransitions.IsValidTransition(from, to));
    }

    [Theory]
    [InlineData(TournamentStatus.Scheduled)]
    [InlineData(TournamentStatus.OpenForRegistration)]
    [InlineData(TournamentStatus.RegistrationClosed)]
    [InlineData(TournamentStatus.Ongoing)]
    [InlineData(TournamentStatus.Finished)]
    [InlineData(TournamentStatus.Canceled)]
    public void IsValidTransition_ToSameStatus_IsTreatedAsAllowedNoOp(TournamentStatus status)
    {
        Assert.True(TournamentStatusTransitions.IsValidTransition(status, status));
    }

    [Theory]
    [InlineData(TournamentStatus.Finished, true)]
    [InlineData(TournamentStatus.Canceled, true)]
    [InlineData(TournamentStatus.Scheduled, false)]
    [InlineData(TournamentStatus.OpenForRegistration, false)]
    [InlineData(TournamentStatus.RegistrationClosed, false)]
    [InlineData(TournamentStatus.Ongoing, false)]
    public void IsTerminal_ReturnsTrueOnlyForFinishedAndCanceled(TournamentStatus status, bool expected)
    {
        Assert.Equal(expected, TournamentStatusTransitions.IsTerminal(status));
    }
}
