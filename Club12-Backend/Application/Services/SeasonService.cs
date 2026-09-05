using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Helper.Slug;

using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services;

/// <summary>
/// Manages seasons, the grouping above tournaments, deleting each one through its own tournaments.
/// </summary>
public class SeasonService(ISeasonRepository seasonRepository, ITournamentService tournamentService) : ISeasonService
{
    public async Task<Season> CreateSeasonAsync(Season seasonEntity)
    {
        seasonEntity.Slug = await SlugGenerator.GenerateUniqueSlugAsync(
            seasonEntity.Name,
            candidate => seasonRepository.ExistsAsync(season => season.Slug == candidate));

        await seasonRepository.AddAsync(seasonEntity);
        return seasonEntity;
    }

    public async Task<Season?> GetSeasonByIdAsync(Guid seasonId)
    {
        return await seasonRepository.GetByIdAsync(seasonId, includes: [season => season.Tournaments]);
    }

    /// <summary>
    /// Retrieves a season by id or slug, auto-detecting which one was passed via GUID parsing.
    /// </summary>
    /// <param name="idOrSlug">The season's GUID id or its slug.</param>
    /// <returns>The matching season, or null if not found.</returns>
    public async Task<Season?> GetSeasonByIdOrSlugAsync(string idOrSlug)
    {
        if (Guid.TryParse(idOrSlug, out Guid seasonId))
        {
            return await GetSeasonByIdAsync(seasonId);
        }

        IEnumerable<Season> matches = await seasonRepository.FindAsync(
            season => season.Slug == idOrSlug,
            includes: [season => season.Tournaments]);

        return matches.FirstOrDefault();
    }

    /// <summary>
    /// Deletes a season and every tournament it groups, one by one through the tournament service.
    /// </summary>
    public async Task DeleteSeasonAsync(Guid id)
    {
        Season? season = await seasonRepository.GetByIdAsync(id, includes: [s => s.Tournaments]);
        if (season is null)
        {
            return;
        }

        foreach (Tournament tournament in season.Tournaments)
        {
            await tournamentService.DeleteTournamentAsync(tournament.Id);
        }

        await seasonRepository.RemoveAsync(s => s.Id == id);
    }

    public async Task UpdateSeasonAsync(Season seasonEntity)
    {
        await seasonRepository.UpdateAsync(seasonEntity);
    }

    public async Task<IEnumerable<Season>> GetAllSeasonsAsync()
    {
        IEnumerable<Season> seasons = await seasonRepository.FindAsync(
            season => true,
            includes: [season => season.Tournaments]);

        // Ordered in memory, newest season first with null years last and Name as a deterministic tiebreak, since the list is small and unpaginated and passing a SortBy through FindAsync would also truncate it to the default page size.
        return seasons
            .OrderByDescending(season => season.Year.HasValue)
            .ThenByDescending(season => season.Year)
            .ThenBy(season => season.Name, StringComparer.OrdinalIgnoreCase);
    }
}
