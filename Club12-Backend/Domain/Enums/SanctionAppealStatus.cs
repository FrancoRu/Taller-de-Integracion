namespace Domain.Enums;

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
    /// The appeal was accepted and the sanction is overturned.
    /// </summary>
    Accepted,

    /// <summary>
    /// The appeal was rejected and the sanction stands.
    /// </summary>
    Rejected
}
