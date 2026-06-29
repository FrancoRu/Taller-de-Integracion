using System.ComponentModel.DataAnnotations;
namespace Application.DTOs.PlayerSanction.Request;

/// <summary>
/// Request DTO for resolving a player sanction appeal.
/// </summary>
public class ResolveAppealRequest
{
    /// <summary>
    /// Whether the appeal is accepted (true) or rejected (false).
    /// </summary>
    [Required]
    public required bool Accepted { get; set; }

    /// <summary>
    /// The decision notes recorded for the appeal resolution.
    /// </summary>
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public required string Resolution { get; set; }
}
