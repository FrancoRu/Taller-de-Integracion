using Domain.Entities.Models;

using System;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for managing Match entities.
/// </summary>
public interface IMatchRepository : IGenericRepository<Match>
{
    /// <summary>
    /// Loads a single match by its id or slug with everything the public match detail needs: both teams, the venue, and the match's scorers with their players.
    /// </summary>
    /// <param name="idOrSlug">The match's GUID id or its slug.</param>
    /// <returns>The match with its detail graph loaded, or null if not found.</returns>
    Task<Match?> GetDetailByIdOrSlugAsync(string idOrSlug);
}
