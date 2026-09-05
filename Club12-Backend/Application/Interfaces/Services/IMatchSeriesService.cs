using Application.DTOs.Abstract.Response;
using Application.DTOs.MatchSeries.Request;

using Domain.Entities.Models;

using System;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Service for managing best-of-N playoff series between two teams at a single bracket round.
/// </summary>
public interface IMatchSeriesService
{
    /// <summary>
    /// Retrieves a series by its identifier, including its games.
    /// </summary>
    /// <param name="seriesId">Unique identifier of the series.</param>
    /// <returns>The found series or null if it does not exist.</returns>
    Task<MatchSeries?> GetSeriesByIdAsync(Guid seriesId);

    /// <summary>
    /// Retrieves a paginated and filtered list of series.
    /// </summary>
    /// <param name="filter">Object containing parameters to filter, sort, and paginate the results.</param>
    /// <returns>A paginated response with the list of series.</returns>
    Task<PaginatedResponse<MatchSeries>> GetAllSeriesAsync(GetMatchSeriesFilteredRequest filter);

    /// <summary>
    /// Creates a new best-of-N series between two teams at a stage, copying the stage's BestOf value onto the series.
    /// </summary>
    /// <param name="stageId">The stage, round, the series belongs to.</param>
    /// <param name="homeTeamId">The home team.</param>
    /// <param name="visitorTeamId">The visitor team.</param>
    /// <returns>The created series entity.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the stage does not exist, the two teams are the same,
    /// either team is not assigned to the stage's division, or a series
    /// between these two teams already exists for this stage.
    /// </exception>
    Task<MatchSeries> CreateSeriesAsync(Guid stageId, Guid homeTeamId, Guid visitorTeamId);

    /// <summary>
    /// Schedules the next game of an existing series.
    /// </summary>
    /// <param name="seriesId">The series to add a game to.</param>
    /// <param name="matchDate">The date of the game.</param>
    /// <param name="venueId">Optional venue for the game.</param>
    /// <returns>The created game, a Match entity.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the series does not exist, is already decided, or
    /// already has BestOf games scheduled.
    /// </exception>
    Task<Match> AddGameToSeriesAsync(Guid seriesId, DateTime matchDate, Guid? venueId);

    /// <summary>
    /// Recomputes and persists the series' winner based on its finished games.
    /// </summary>
    /// <param name="seriesId">The series to recompute.</param>
    Task RecalculateSeriesWinnerAsync(Guid seriesId);
}
