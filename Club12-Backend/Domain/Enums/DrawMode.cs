namespace Domain.Enums;

/// <summary>
/// How a playoffs-only bracket's first-round team order is produced: shuffled server-side or set by an admin.
/// </summary>
public enum DrawMode
{
    Random,
    Manual,
}
