namespace Application.DTOs.Scorer.Response;

public class ScorerBaseResponse
{

    /// <summary>
    /// Total points scored, aggregated over whatever this response is grouped
    /// by — a player or a team — not just a single match.
    /// </summary>
    public required int Points { get; set; }
}
