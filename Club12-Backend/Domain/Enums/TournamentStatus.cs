namespace Domain.Enums;

/// <summary>
/// Lifecycle status of a <see cref="Domain.Entities.Models.Tournament"/>. See
/// <see cref="TournamentStatusTransitions"/> for the allowed moves between
/// these values, including the Ongoing -> RegistrationClosed revert path.
/// </summary>
public enum TournamentStatus
{
    /// <summary>
    /// The tournament is scheduled but not yet open for team registrations.
    /// </summary>
    Scheduled,
    OpenForRegistration,
    /// <summary>
    /// Registration has closed. Structural changes (divisions, stages, team
    /// registrations) are frozen: the roster is fixed and teams are assigned to
    /// divisions. The fixture is NOT generated yet — it is generated when the
    /// tournament starts (transition to <see cref="Ongoing"/>). The tournament
    /// is waiting to start.
    /// </summary>
    RegistrationClosed,
    Ongoing,
    Finished,
    /// <summary>
    /// The tournament has been canceled and will not take place.
    /// </summary>
    Canceled,
}
