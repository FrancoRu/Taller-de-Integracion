using Club12.Viewmodels.Abstract;

namespace Club12.Viewmodels.Team;

/// <summary>
/// Represents a response for a team, inheriting from the base response.
/// </summary>
public class TeamResponse : BaseResponse
{
    /// <summary>
    /// The name of the team.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The three-letter code of the team.
    /// </summary>
    public required string ThreeLetterCode { get; set; }

    /// <summary>
    /// The unique identifier of the division to which the team belongs.
    /// </summary>
    public required string DivisionId { get; set; }
}
