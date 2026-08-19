namespace Application.DTOs.DataMaintenance.Response;

/// <summary>
/// Row counts removed by DataMaintenanceService.WipeSampleDataAsync, for
/// the admin UI's success summary. Identity (users, roles) is never
/// touched by the wipe, so it has no counters here.
/// </summary>
public sealed record DataWipeResult(
    int Tournaments,
    int Divisions,
    int Teams,
    int Players,
    int Matches,
    int MatchSeries,
    int PlayerSanctions,
    int PlayerStatistics,
    int Scorers,
    int StageTeamMatches,
    int PlayerTeamRegistrations,
    int Stages,
    int Venues,
    int BlogPosts
);
