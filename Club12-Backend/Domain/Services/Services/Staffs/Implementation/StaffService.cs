using Entities.Models.Staffs;

using Microsoft.EntityFrameworkCore;

using Services.DataAccessLayer.GenericEntity;

namespace Services.Services.Staffs.Implementation;

/// <summary>
/// Initializes a new instance of the <see cref="StaffService"/> class.
/// </summary>
/// <param name="_genericStaffService">The generic service to handle staff data.</param>
public class StaffService(IGenericService<Staff> _genericStaffService) : IStaffService
{

    /// <summary>
    /// Creates a new staff member asynchronously.
    /// </summary>
    /// <param name="staffEntity">The staff entity to create.</param>
    /// <returns>The created staff member.</returns>
    public async Task<Staff> CreateStaffAsync(Staff staffEntity)
    {
        await _genericStaffService.InsertAsync(staffEntity);
        return staffEntity;
    }

    /// <summary>
    /// Retrieves a staff member by its ID asynchronously.
    /// </summary>
    /// <param name="staffId">The ID of the staff member to retrieve.</param>
    /// <returns>The staff member with the specified ID, or null if not found.</returns>
    public async Task<Staff?> GetStaffByIdAsync(Guid staffId) => await _genericStaffService.FilterByExpression(staff => staff.Id == staffId).FirstOrDefaultAsync();

    /// <summary>
    /// Deletes a staff member asynchronously.
    /// </summary>
    /// <param name="staffEntity">The staff member to delete.</param>
    /// <returns>A boolean indicating whether the deletion was successful.</returns>
    public async Task<bool> DeleteStaffAsync(Staff staffEntity)
    {
        try
        {
            await _genericStaffService.DeleteAsync(staffEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Updates a staff member asynchronously.
    /// </summary>
    /// <param name="staffEntity">The staff member to update.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    public async Task<bool> UpdateStaffAsync(Staff staffEntity)
    {
        try
        {
            await _genericStaffService.UpdateAsync(staffEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
