using Application.Utils.Constants.Validation;

using System.ComponentModel.DataAnnotations;
namespace Application.DTOs.PlayerSanction.Request;

/// <summary>
/// Request DTO for submitting an appeal against a player sanction.
/// </summary>
public class AppealPlayerSanctionRequest
{
    /// <summary>
    /// The reason the sanction is being appealed.
    /// </summary>
    [Required]
    [StringLength(SanctionFieldLengths.LongTextMaxLength, MinimumLength = SanctionFieldLengths.LongTextMinLength)]
    public required string Reason { get; set; }
}
