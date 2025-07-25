namespace Entities.Models.Stages;

/// <summary>
/// Represents the types of stages within a tournament.
/// </summary>
public enum StageType
{
    /// <summary>
    /// Group stage, where teams play in a round-robin format.
    /// </summary>
    Group,

    /// <summary>
    /// Round of 16 stage (also known as "Octavos de final").
    /// </summary>
    RoundOf16,

    /// <summary>
    /// Quarter-final stage.
    /// </summary>
    QuarterFinal,

    /// <summary>
    /// Semi-final stage.
    /// </summary>
    SemiFinal,

    /// <summary>
    /// Third place match stage.
    /// </summary>
    ThirdPlace,

    /// <summary>
    /// Final match stage.
    /// </summary>
    Final
}
