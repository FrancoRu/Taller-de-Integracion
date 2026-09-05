namespace Application.Utils.Constants.Stage;

public static class StageTemplate
{
    public static readonly Template Group = new("Fase de Grupos", "Fase de grupos todos contra todos.");

    public static readonly Template RoundOf16 = new("Octavos de Final", "Etapa eliminatoria con 16 equipos.");

    public static readonly Template QuarterFinal = new("Cuartos de Final", "Etapa eliminatoria con 8 equipos.");

    public static readonly Template SemiFinal = new("Semifinales", "Etapa eliminatoria con 4 equipos.");

    public static readonly Template Final = new("Final", "Partido final del campeonato.");

    public static readonly Template ThirdPlace = new("Tercer Puesto", "Partido para determinar el tercer lugar.");

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