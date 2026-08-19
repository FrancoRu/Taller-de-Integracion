namespace Domain.Enums;

/// <summary>
/// Represents the appeal state of a player sanction.
/// </summary>
public enum SanctionAppealStatus
{
    /// <summary>
    /// No appeal has been submitted.
    /// </summary>
    None,

    /// <summary>
    /// An appeal was submitted and is awaiting a decision.
    /// </summary>
    Pending,

    /// <summary>
    /// The appeal was accepted (the sanction is overturned).
    /// </summary>
    Accepted,

    /// <summary>
    /// The appeal was rejected (the sanction stands).
    /// </summary>
    Rejected
}
