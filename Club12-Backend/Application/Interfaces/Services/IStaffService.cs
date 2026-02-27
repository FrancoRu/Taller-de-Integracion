using Domain.Entities.Models;
using System;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Represents a service for managing staff.
/// </summary>
public interface IStaffService
{
    /// <summary>
    /// Creates a new staff member.
    /// </summary>
    /// <param name="staffEntity">The staff entity to create.</param>
    /// <returns>The created staff member.</returns>
    Task<Staff> CreateStaffAsync(Staff staffEntity);

    /// <summary>
    /// Retrieves a staff member by its ID.
    /// </summary>
    /// <param name="staffId">The ID of the staff member to retrieve.</param>
    /// <returns>The staff member with the specified ID, or null if not found.</returns>
    Task<Staff?> GetStaffByIdAsync(Guid staffId);

    /// <summary>
    /// Deletes a staff member.
    /// </summary>
    /// <param name="staffEntity">The staff member to delete.</param>
    /// <returns>A boolean indicating whether the deletion was successful.</returns>
    Task DeleteStaffAsync(Guid id);

    /// <summary>
    /// Updates a staff member asynchronously.
    /// </summary>
    /// <param name="staffEntity">The staff member to update.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    Task UpdateStaffAsync(Staff staffEntity);
}
