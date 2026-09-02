using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Helper.Slug;

using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services;

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
    /// Retrieves a season by its id or its slug. The value is treated as an id
    /// when it parses as a GUID, otherwise it is looked up as a slug.
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
    /// Deletes a Season and every tournament it groups. The DB-level
    /// Season→Tournament relationship is SetNull, not Cascade (a season is a
    /// purely additive grouping and must never silently wipe a tournament's
    /// history), so a bare bulk delete of the Season row would only detach
    /// its tournaments — leaving them alive but orphaned (no season, yet
    /// still enrolling teams, still blocking those teams from being
    /// deleted, and invisible from any season-scoped screen). Routing
    /// through <see cref="ITournamentService.DeleteTournamentAsync"/> for
    /// each tournament reuses its own history guard (so a season with a
    /// started/played tournament fails loudly with the real reason, instead
    /// of the season vanishing while an orphan tournament lingers) and its
    /// cascaded cleanup (team-tournament registrations, etc.).
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
        return await seasonRepository.FindAsync(
            season => true,
            includes: [season => season.Tournaments]);
    }
}
