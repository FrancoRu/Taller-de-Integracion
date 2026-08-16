using Application.Utils.Constants.Validation;
using System.ComponentModel.DataAnnotations;
namespace Application.DTOs.PlayerSanction.Request;

/// <summary>
/// Represents a request to update a Player Sanction.
/// </summary>
public class UpdatePlayerSanctionRequest
{
    /// <summary>
    /// The duration in fixtures of the sanction.
    /// </summary>
    public int? Duration { get; set; }

    /// <summary>
    /// A description of the sanction.
    /// </summary>
    [MaxLength(SanctionFieldLengths.DescriptionMaxLength)]
    public string? Description { get; set; }
}
