namespace Entities.DTOs.Player;

/// <summary>
/// Represents a request to get filtered players for admin access.
/// </summary>
public class GetPlayersFilteredRequest : PlayerFilterRequestBase
{
    /// <summary>
    /// The document number of the player to filter by.
    /// </summary>
    public string? DocumentNumber { get; set; }

    /// <summary>
    /// The birthdate of the player to filter by.
    /// </summary>
    public DateTime? BirthDate { get; set; }

    /// <summary>
    /// The phone number of the player to filter by.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// The medical social work or health provider of the player to filter by.
    /// </summary>
    public string? SocialSecurity { get; set; }

    /// <summary>
    /// Indicates if the player is federated.
    /// </summary>
    public bool? IsFederated { get; set; }

    /// <summary>
    /// The club the player belongs to, if federated, to filter by.
    /// </summary>
    public string? Club { get; set; }

    /// <summary>
    /// The category the player belongs to, if federated, to filter by.
    /// </summary>
    public string? Category { get; set; }
}
