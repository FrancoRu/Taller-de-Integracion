using Application.DTOs.DataMaintenance.Response;

using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Admin-only tools for resetting the tournament-domain data to a clean, realistic sample state.
/// </summary>
public interface IDataMaintenanceService
{
    /// <summary>
    /// Deletes every tournament-domain row inside one transaction, leaving Identity untouched.
    /// </summary>
    Task<DataWipeResult> WipeSampleDataAsync(CancellationToken ct = default);

    /// <summary>
    /// Seeds 2 complete, distinct sample tournaments.
    /// </summary>
    Task<DataSeedResult> SeedSampleDataAsync(CancellationToken ct = default);
}
