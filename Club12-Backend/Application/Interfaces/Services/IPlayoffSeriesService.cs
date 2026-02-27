using Domain.Entities.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Represents a service for managing playoff series.
/// </summary>
public interface IPlayoffSeriesService
{
    /// <summary>
    /// Creates playoff series for each round and links them together.
    /// </summary>
    /// <returns>A list of created playoff series.</returns>
    Task<List<PlayoffSerie>> CreatePlayoffSeriesAsync();

    /// <summary>
    /// Gets a playoff series by its ID.
    /// </summary>
    /// <param name="id">The ID of the playoff series.</param>
    /// <returns>The playoff series, or null if not found.</returns>
    Task<PlayoffSerie?> GetSeriesByIdAsync(Guid id);

    /// <summary>
    /// Updates a playoff series.
    /// </summary>
    /// <param name="series">The playoff series to update.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    Task<bool> UpdateSeriesAsync(PlayoffSerie series);

    /// <summary>
    /// Deletes a playoff series.
    /// </summary>
    /// <param name="series">The playoff series to delete.</param>
    /// <returns>True if the deletion was successful; otherwise, false.</returns>
    Task<bool> DeleteSeriesAsync(PlayoffSerie series);
}