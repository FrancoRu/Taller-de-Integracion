namespace Application.Utils.Constants.Stage;

/// <summary>
/// Valid total registered-team counts for automated bracket generation.
/// A tournament's registered team count must be one of these values (a power
/// of two) to be split into a valid group + knockout stage tree.
/// </summary>
public static class TournamentBracketSize
{
    public const int EIGHT = 8;
    public const int SIXTEEN = 16;
    public const int THIRTY_TWO = 32;
    public const int SIXTY_FOUR = 64;
}
