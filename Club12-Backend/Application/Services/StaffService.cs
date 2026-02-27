using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Application.Services;

/// <summary>
/// Initializes a new instance of the <see cref="StaffService"/> class.
/// </summary>
/// <param name="staffRepository">The generic service to handle staff data.</param>
public class StaffService(IStaffRepository staffRepository) : IStaffService
{

    /// <summary>
    /// Creates a new staff member asynchronously.
    /// </summary>
    /// <param name="staffEntity">The staff entity to create.</param>
    /// <returns>The created staff member.</returns>
    public async Task<Staff> CreateStaffAsync(Staff staffEntity)
    {
        await staffRepository.AddAsync(staffEntity);
        return staffEntity;
    }

    /// <summary>
    /// Retrieves a staff member by its ID asynchronously.
    /// </summary>
    /// <param name="staffId">The ID of the staff member to retrieve.</param>
    /// <returns>The staff member with the specified ID, or null if not found.</returns>
    public async Task<Staff?> GetStaffByIdAsync(Guid staffId)
        => await staffRepository.GetByIdAsync(staffId);

    /// <summary>
    /// Deletes a staff member asynchronously.
    /// </summary>
    /// <param name="staffEntity">The staff member to delete.</param>
    /// <returns>A boolean indicating whether the deletion was successful.</returns>
    public async Task DeleteStaffAsync(Guid id)
        => await staffRepository.RemoveAsync(staff => staff.Id == id);
    

    /// <summary>
    /// Updates a staff member asynchronously.
    /// </summary>
    /// <param name="staffEntity">The staff member to update.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    public async Task UpdateStaffAsync(Staff staffEntity)
        => await staffRepository.UpdateAsync(staffEntity);
}
