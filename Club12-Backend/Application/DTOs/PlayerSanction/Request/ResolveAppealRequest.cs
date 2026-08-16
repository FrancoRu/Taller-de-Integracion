using Application.Utils.Constants.Validation;
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
    [StringLength(SanctionFieldLengths.LongTextMaxLength, MinimumLength = SanctionFieldLengths.LongTextMinLength)]
    public required string Resolution { get; set; }
}
