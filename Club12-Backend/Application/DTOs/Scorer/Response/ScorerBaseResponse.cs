namespace Application.DTOs.Scorer.Response;

public class ScorerBaseResponse
{

    /// <summary>
    /// Number of points scored by the team in the match.
    /// </summary>
    public required int Points { get; set; }
}
