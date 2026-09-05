using Application.DTOs.Abstract.Response;
namespace Application.DTOs.Divisions.Response;

/// <summary>
/// Represents a minimal response for a tournament.
/// </summary>
public class MinimalDivisionResponse : BaseEntityResponse
{
    public required string Name { get; set; }

    public required bool IsFinished { get; set; }
}
