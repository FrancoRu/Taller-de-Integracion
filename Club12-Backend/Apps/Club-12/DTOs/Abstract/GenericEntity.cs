namespace Club12.Viewmodels.Abstract;

/// <summary>
/// Base response class containing common properties for responses.
/// </summary>
public class GenericEntity
{
    /// <summary>
    /// The unique identifier of the entity.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// The date and time when the entity was created.
    /// </summary>
    public required DateTime DateCreated { get; set; }

    /// <summary>
    /// The date and time when the entity was last updated.
    /// </summary>
    public required DateTime DateUpdated { get; set; }
}
