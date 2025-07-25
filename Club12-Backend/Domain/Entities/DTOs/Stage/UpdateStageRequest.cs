namespace Entities.DTOs.Stage;

/// <summary>
/// Represents the payload for updating an existing stage.
/// </summary>
public class UpdateStageRequest
{
    /// <summary>
    /// Optional description providing additional details about the stage.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Indicates whether the stage is currently active.
    /// If null, the default should be true.
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Optional new Division ID if the stage is moved to a different division.
    /// </summary>
    public int? DivisionId { get; set; }
}
