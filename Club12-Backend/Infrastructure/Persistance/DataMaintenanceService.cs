using Application.DTOs.DataMaintenance.Response;
using Application.Interfaces.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Persistance;

/// <inheritdoc cref="IDataMaintenanceService"/>
public sealed class DataMaintenanceService(ApplicationDBContext db, ILogger<DataMaintenanceService> logger)
    : IDataMaintenanceService
{
    public async Task<DataWipeResult> WipeSampleDataAsync(CancellationToken ct = default)
    {
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await db.Database.BeginTransactionAsync(ct);

        try
        {
            int scorers = await db.Scorers.ExecuteDeleteAsync(ct);
            int playerStatistics = await db.PlayersStatistics.ExecuteDeleteAsync(ct);
            int playerSanctions = await db.PlayerSanctions.ExecuteDeleteAsync(ct);
            int stageTeamMatches = await db.StageTeamMatches.ExecuteDeleteAsync(ct);
            int playerTeamRegistrations = await db.PlayerTeamRegistrations.ExecuteDeleteAsync(ct);
            int matches = await db.Matches.ExecuteDeleteAsync(ct);
            int matchSeries = await db.MatchSeries.ExecuteDeleteAsync(ct);
            int players = await db.Players.ExecuteDeleteAsync(ct);
            int stages = await db.Stages.ExecuteDeleteAsync(ct);
            int teams = await db.Teams.ExecuteDeleteAsync(ct);
            int divisions = await db.Divisions.ExecuteDeleteAsync(ct);
            int tournaments = await db.Tournaments.ExecuteDeleteAsync(ct);
            int venues = await db.Venues.ExecuteDeleteAsync(ct);
            int blogPosts = await db.BlogPosts.ExecuteDeleteAsync(ct);

            await transaction.CommitAsync(ct);

            logger.LogInformation(
                "Sample data wiped: {TournamentCount} tournaments, {DivisionCount} divisions, " +
                "{TeamCount} teams, {PlayerCount} players, {MatchCount} matches, {BlogPostCount} blog posts.",
                tournaments, divisions, teams, players, matches, blogPosts);

            return new DataWipeResult(
                Tournaments: tournaments,
                Divisions: divisions,
                Teams: teams,
                Players: players,
                Matches: matches,
                MatchSeries: matchSeries,
                PlayerSanctions: playerSanctions,
                PlayerStatistics: playerStatistics,
                Scorers: scorers,
                StageTeamMatches: stageTeamMatches,
                PlayerTeamRegistrations: playerTeamRegistrations,
                Stages: stages,
                Venues: venues,
                BlogPosts: blogPosts);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public Task<DataSeedResult> SeedSampleDataAsync(CancellationToken ct = default) =>
        throw new NotImplementedException("Implemented in Task 3.");
}
