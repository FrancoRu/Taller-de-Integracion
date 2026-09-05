namespace Application.DTOs.DataMaintenance.Response;

/// <summary>
/// Row counts removed by the sample-data wipe, for the admin UI's success summary.
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
