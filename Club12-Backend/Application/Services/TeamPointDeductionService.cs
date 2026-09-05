using Application.Interfaces.Repositories;
using Application.Interfaces.Services;

using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services;

/// <summary>
/// Service managing disciplinary point deductions for teams within a division.
/// </summary>
public class TeamPointDeductionService(
    ITeamPointDeductionRepository deductionRepository) : ITeamPointDeductionService
{
    /// <inheritdoc/>
    public async Task<TeamPointDeduction> CreateAsync(TeamPointDeduction deduction)
    {
        await deductionRepository.AddAsync(deduction);

        // Reload with the team navigation so the response can name the team.
        return await deductionRepository.GetByIdAsync(
            deduction.Id,
            includes: [entity => entity.Team!]) ?? deduction;
    }

    /// <inheritdoc/>
    public async Task<List<TeamPointDeduction>> GetByDivisionIdAsync(Guid divisionId)
    {
        IEnumerable<TeamPointDeduction> deductions = await deductionRepository.FindAsync(
            deduction => deduction.DivisionId == divisionId,
            includes: [deduction => deduction.Team!]);

        return [.. deductions.OrderByDescending(deduction => deduction.DateCreated)];
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id)
    {
        await deductionRepository.RemoveAsync(deduction => deduction.Id == id);
    }
}
