using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Manages disciplinary point deductions, deducción de puntos, applied to teams within a division.
/// </summary>
public interface ITeamPointDeductionService
{
    /// <summary>
    /// Creates a new point deduction.
    /// </summary>
    /// <param name="deduction">The deduction to create.</param>
    /// <returns>The created deduction, with the team navigation loaded.</returns>
    Task<TeamPointDeduction> CreateAsync(TeamPointDeduction deduction);

    /// <summary>
    /// Returns every point deduction for a division, newest first, with each deduction's team loaded so the caller can show the team's name.
    /// </summary>
    /// <param name="divisionId">The division whose deductions to list.</param>
    Task<List<TeamPointDeduction>> GetByDivisionIdAsync(Guid divisionId);

    /// <summary>
    /// Deletes a point deduction by its id. No-op when it does not exist.
    /// </summary>
    /// <param name="id">The id of the deduction to remove.</param>
    Task DeleteAsync(Guid id);
}
