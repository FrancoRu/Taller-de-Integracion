namespace Application.Utils.Constants.Stage;

/// <summary>
/// Provides predefined templates and constants for tournament stages.
/// </summary>
public static class StageTemplate
{
    /// <summary>
    /// Template for the group stage, where all teams play against each other.
    /// </summary>
    public static readonly Template Group = new("Fase de Grupos", "Fase de grupos todos contra todos.");

    /// <summary>
    /// Template for the quarter-final stage, an elimination round with 8 teams.
    /// </summary>
    public static readonly Template QuarterFinal = new("Cuartos de Final", "Etapa eliminatoria con 8 equipos.");

    /// <summary>
    /// Template for the semi-final stage, an elimination round with 4 teams.
    /// </summary>
    public static readonly Template SemiFinal = new("Semifinales", "Etapa eliminatoria con 4 equipos.");

    /// <summary>
    /// Template for the final match of the tournament.
    /// </summary>
    public static readonly Template Final = new("Final", "Partido final del campeonato.");

    /// <summary>
    /// Template for the third place match, determining the third ranked team.
    /// </summary>
    public static readonly Template ThirdPlace = new("Tercer Puesto", "Partido para determinar el tercer lugar.");

    /// <summary>
    /// The default duration in days for a stage.
    /// </summary>
    public static readonly int DurationDays = 7;

    /// <summary>
    /// Standard rest gap, in days, inserted between two consecutive
    /// automated stages (e.g. group stage to quarter-final, quarter-final
    /// to semi-final, third place to final).
    /// </summary>
    public static readonly int StandardGapDays = 2;

    /// <summary>
    /// Shorter rest gap, in days, inserted between the semi-final and the
    /// third place match.
    /// </summary>
    public static readonly int ThirdPlaceGapDays = 1;
}