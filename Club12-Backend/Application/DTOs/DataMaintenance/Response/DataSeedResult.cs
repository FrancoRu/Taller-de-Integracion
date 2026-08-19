namespace Application.DTOs.DataMaintenance.Response;

/// <summary>
/// Row counts created by DataMaintenanceService.SeedSampleDataAsync, for
/// the admin UI's success summary.
/// </summary>
public sealed record DataSeedResult(
    int Tournaments,
    int Divisions,
    int Teams,
    int Players,
    int Matches,
    int PlayerSanctions,
    int BlogPosts
);
