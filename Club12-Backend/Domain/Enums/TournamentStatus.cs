namespace Domain.Enums;

public enum TournamentStatus
{
    /// <summary>
    /// The tournament is scheduled but not yet open for team registrations.
    /// </summary>
    Scheduled,
    /// <summary>
    /// The tournament is open for team registrations.
    /// </summary>
    OpenForRegistration,
    /// <summary>
    /// The tournament is currently ongoing.
    /// </summary>
    Ongoing,
    /// <summary>
    /// The tournament has finished.
    /// </summary>
    Finished,
    /// <summary>
    /// The tournament has been canceled and will not take place.
    /// </summary>
    Canceled,
}
