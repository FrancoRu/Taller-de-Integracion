namespace Domain.Enums;

/// <summary>
/// The role a member of a team's technical staff (cuerpo técnico) holds for a
/// given season.
/// </summary>
public enum TeamStaffRole
{
    /// <summary>
    /// Head coach (Director Técnico / DT).
    /// </summary>
    Coach,

    /// <summary>
    /// Assistant coach (Asistente).
    /// </summary>
    AssistantCoach,

    /// <summary>
    /// A player who also coaches the team (DT-Jugador).
    /// </summary>
    PlayerCoach
}
