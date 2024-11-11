using Club12.Viewmodels.Abstract;

namespace Club12.Viewmodels.Division;

/// <summary>
/// Represents a response for a division, inheriting from the base response.
/// </summary>
public class DivisionResponse : BaseResponse
{
    /// <summary>
    /// The name of the division.
    /// </summary>
    public required string Name { get; set; }
}
