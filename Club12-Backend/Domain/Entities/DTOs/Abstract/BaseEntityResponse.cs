namespace Entities.DTOs.Abstract;

/// <summary>
/// Base response class containing common properties for responses.
/// </summary>
public class BaseEntityResponse
{
    /// <summary>
    /// The unique identifier of the entity.
    /// </summary>
    public required string Id { get; set; }
}
