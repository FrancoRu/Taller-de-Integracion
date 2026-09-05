using Application.DTOs.Champions.Response;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Computes the champion and podium of each competition, a zone division or the cross-division cup.
/// </summary>
public interface IChampionService
{
    /// <summary>
    /// Computes a division's podium from the playoff final and third-place match when the division has a playoff, or from the group-phase standings otherwise.
    /// </summary>
    /// <param name="divisionId">The id of the division.</param>
    /// <returns>The division's podium, or null when the division does not exist.</returns>
    Task<PodiumResponse?> GetDivisionPodiumAsync(Guid divisionId);

    /// <summary>
    /// Computes the podium of every division of a tournament so the caller sees the whole tournament at a glance.
    /// </summary>
    /// <param name="tournamentId">The id of the tournament.</param>
    /// <returns>One podium per division; empty when the tournament has no divisions.</returns>
    Task<List<PodiumResponse>> GetTournamentChampionsAsync(Guid tournamentId);

    /// <summary>
    /// Returns the champion, 1st place, of every division of every FINISHED tournament, optionally scoped to a single season.
    /// </summary>
    /// <param name="seasonId">Optional season filter; when null, spans all seasons.</param>
    /// <returns>One row per crowned division champion.</returns>
    Task<List<ChampionHistoryResponse>> GetChampionsHistoryAsync(Guid? seasonId);
}
