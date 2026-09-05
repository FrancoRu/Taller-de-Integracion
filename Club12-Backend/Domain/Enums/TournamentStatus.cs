namespace Domain.Enums;

/// <summary>
/// Lifecycle status of a Tournament; see TournamentStatusTransitions for the allowed moves between these values.
/// </summary>
public enum TournamentStatus
{
    /// <summary>
    /// The tournament is scheduled but not yet open for team registrations.
    /// </summary>
    Scheduled,
    OpenForRegistration,
    /// <summary>
    /// Registration has closed, freezing structural changes: the roster is fixed and teams are assigned to divisions.
    /// </summary>
    RegistrationClosed,
    Ongoing,
    Finished,
    /// <summary>
    /// The tournament has been canceled and will not take place.
    /// </summary>
    Canceled,
}
