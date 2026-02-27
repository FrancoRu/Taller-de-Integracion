namespace Application.DTOs.Stage.Request;

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
}
