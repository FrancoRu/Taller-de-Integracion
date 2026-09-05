using Application.Utils.Constants.Validation;

using System.ComponentModel.DataAnnotations;
namespace Application.DTOs.PlayerSanction.Request;

/// <summary>
/// Request DTO for resolving a player sanction appeal.
/// </summary>
public class ResolveAppealRequest
{
    [Required]
    public required bool Accepted { get; set; }

    [Required]
    [StringLength(SanctionFieldLengths.LongTextMaxLength, MinimumLength = SanctionFieldLengths.LongTextMinLength)]
    public required string Resolution { get; set; }
}
