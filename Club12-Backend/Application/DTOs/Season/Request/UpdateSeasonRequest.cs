using Application.Utils.Constants.Validation;

using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Season.Request;

/// <summary>
/// Request model for updating an existing season ("Temporada").
/// </summary>
public class UpdateSeasonRequest
{
    /// <summary>
    /// The display name of the season, e.g. "Temporada XXVII".
    /// </summary>
    [Required(ErrorMessage = "The Name field is required.")]
    [MaxLength(SeasonFieldLengths.NameMaxLength)]
    public required string Name { get; set; }

    /// <summary>
    /// The calendar year the season is played in (optional).
    /// </summary>
    public int? Year { get; set; }
}
