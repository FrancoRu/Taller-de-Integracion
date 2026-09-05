namespace Application.Utils.Constants.Validation;

/// <summary>
/// Field length limits shared by CreateVenueRequest and UpdateVenueRequest, so the two stay in sync.
/// </summary>
public static class VenueFieldLengths
{
    public const int NameMaxLength = 50;
    public const int AddressMaxLength = 200;
}
