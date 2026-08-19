using Application.DTOs.DataMaintenance.Response;

using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Admin-only tools for resetting the tournament-domain data to a clean,
/// realistic sample state (Supabase dev database use case) — see
/// docs/superpowers/specs/2026-08-18-admin-test-data-tools-design.md.
/// Never touches Identity (users, roles).
/// </summary>
public interface IDataMaintenanceService
{
    /// <summary>
    /// Deletes every tournament-domain row (tournaments, divisions, teams,
    /// players, matches, sanctions, statistics, venues, blog posts) inside
    /// one transaction. Identity is untouched.
    /// </summary>
    Task<DataWipeResult> WipeSampleDataAsync(CancellationToken ct = default);

    /// <summary>
    /// Seeds 2 complete, distinct sample tournaments. Throws
    /// <see cref="System.InvalidOperationException"/> if any tournament
    /// already exists — call <see cref="WipeSampleDataAsync"/> first.
    /// </summary>
    Task<DataSeedResult> SeedSampleDataAsync(CancellationToken ct = default);
}
