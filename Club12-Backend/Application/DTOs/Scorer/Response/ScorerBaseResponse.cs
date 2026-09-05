namespace Application.DTOs.Scorer.Response;

public class ScorerBaseResponse
{

    /// <summary>
    /// Total points scored, aggregated over the grouping this response represents, not just one match.
    /// </summary>
    public required int Points { get; set; }
}
