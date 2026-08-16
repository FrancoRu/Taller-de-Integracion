namespace Application.Utils.Constants.Validation;

/// <summary>
/// Valid range for CreateTournamentRequest.MaxTeams and
/// CreateTournamentRequest.MinTeams.
/// </summary>
public static class TournamentFieldRange
{
    public const int MinAllowedTeams = 4;
    public const int MaxAllowedTeams = 32;
}
