using System;

namespace Application.Interfaces.Maintenance;

/// <summary>
/// Raised when a caller tries to start a backup/restore while one is already
/// running (HU-92). Carries the in-progress <see cref="Status"/> so the API
/// can report what is running and since when. Callers translate this into a
/// 503 "operation in progress" response rather than letting it crash.
/// </summary>
public sealed class MaintenanceInProgressException : Exception
{
    public MaintenanceInProgressException(MaintenanceStatus? status)
        : base("A maintenance operation is already in progress.")
    {
        Status = status;
    }

    public MaintenanceStatus? Status { get; }
}
