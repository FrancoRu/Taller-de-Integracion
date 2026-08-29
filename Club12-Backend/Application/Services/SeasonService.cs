using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Helper.Slug;

using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services;

public class SeasonService(ISeasonRepository seasonRepository) : ISeasonService
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

    public async Task DeleteSeasonAsync(Guid id)
    {
        await seasonRepository.RemoveAsync(season => season.Id == id);
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
