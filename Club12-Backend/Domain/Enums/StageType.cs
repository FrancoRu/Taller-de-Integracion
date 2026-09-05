namespace Domain.Enums;

public enum StageType
{
    /// <summary>
    /// Group stage, where teams play in a round-robin format.
    /// </summary>
    Group,

    /// <summary>
    /// Round of 16 stage.
    /// </summary>
    RoundOf16,

    QuarterFinal,
    SemiFinal,
    ThirdPlace,
    Final
}
