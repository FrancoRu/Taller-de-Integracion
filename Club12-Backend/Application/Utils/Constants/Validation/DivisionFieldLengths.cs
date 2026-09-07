namespace Application.Utils.Constants.Validation;

/// <summary>
/// Field length limits shared by every division-name-accepting request DTO and the Division entity
/// configuration, so they stay in sync. Matches Stage.Name's own 100-character cap, since a division's
/// name is never more constrained than the stages nested under it.
/// </summary>
public static class DivisionFieldLengths
{
    public const int NameMaxLength = 100;
}
