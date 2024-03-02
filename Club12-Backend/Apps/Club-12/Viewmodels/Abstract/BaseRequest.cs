using System.ComponentModel.DataAnnotations;

namespace Club12.Viewmodels.Abstract;

/// <summary>
/// Represents a base request object.
/// </summary>
public class BaseRequest
{
    /// <summary>
    /// The Id of the user that made the request.
    /// </summary>
    [Required(ErrorMessage = "The UserId field is required.")]
    public required Guid UserRequestId { get; set; }
}
