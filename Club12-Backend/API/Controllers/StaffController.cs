using AutoMapper;

using Entities.DTOs.Staff;
using Entities.Models.Staffs;
using Entities.Models.Teams;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Services.Services.Staffs;
using Services.Services.Teams;

namespace Club12.API.Controllers;

/// <summary>
/// Controller for managing Staff members.
/// <param name="_staffService">The staff service for handling staff-related operations.</param>
/// <param name="_teamService">The team service for managing teams associated with staff.</param>
/// <param name="_mapper">The AutoMapper instance for mapping data models.</param>
/// </summary>
//[Authorize(Roles = "SuperAdmin")]
[Route("api/staff/")]
[ApiController]
public class StaffController(IStaffService _staffService, ITeamService _teamService, IMapper _mapper) : ControllerBase
{

    /// <summary>
    /// Creates a new staff member.
    /// </summary>
    /// <param name="staffRequest">The staff request.</param>
    /// <returns>The created staff member response.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(StaffResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StaffResponse>> CreateStaffAsync(CreateStaffRequest staffRequest)
    {
        Guid TeamId = staffRequest.TeamId;
        Team? existingTeam = await _teamService.GetTeamByIdAsync(TeamId);

        if (existingTeam is null)
        {
            return BadRequest($"There is no Team with id: {TeamId}.");
        }

        if (existingTeam.Staff.Count >= 3)
        {
            return BadRequest("You cannot add more staff members.");
        }

        Staff mappedStaff = _mapper.Map<Staff>(staffRequest);
        Staff createdStaff = await _staffService.CreateStaffAsync(mappedStaff);
        StaffResponse staffResponse = _mapper.Map<StaffResponse>(createdStaff);

        return new ObjectResult(staffResponse) { StatusCode = StatusCodes.Status201Created };
    }

    /// <summary>
    /// Retrieves a staff member by its id.
    /// </summary>
    /// <param name="id">The id of the staff member to retrieve.</param>
    /// <returns>The staff member with the specified id.</returns>
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StaffResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StaffResponse>> GetStaffByIdAsync(Guid id)
    {
        Staff? staff = await _staffService.GetStaffByIdAsync(id);

        if (staff is null)
        {
            return BadRequest($"Staff with id {id} not found.");
        }

        StaffResponse staffResponse = _mapper.Map<StaffResponse>(staff);
        return Ok(staffResponse);
    }

    /// <summary>
    /// Updates a staff member by its id.
    /// </summary>
    /// <param name="id">The id of the staff member to update.</param>
    /// <param name="staffRequest">The staff request.</param>
    /// <returns>The updated staff response.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> UpdateStaffAsync(Guid id, UpdateStaffRequest staffRequest)
    {
        Staff? existingStaff = await _staffService.GetStaffByIdAsync(id);

        if (existingStaff is null)
        {
            return BadRequest($"Staff with id {id} not found.");
        }

        _mapper.Map(staffRequest, existingStaff);
        bool updateResult = await _staffService.UpdateStaffAsync(existingStaff);

        return !updateResult ? BadRequest("Failed to update the staff.") : NoContent();
    }

    /// <summary>
    /// Deletes a staff member by its id.
    /// </summary>
    /// <param name="id">The id of the staff member to delete.</param>
    /// <returns>200 (OK) if the staff member was successfully deleted.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteStaffByIdAsync(Guid id)
    {
        Staff? staff = await _staffService.GetStaffByIdAsync(id);

        if (staff is null)
        {
            return BadRequest($"Staff with id {id} not found.");
        }

        bool deleteResult = await _staffService.DeleteStaffAsync(staff);
        return !deleteResult ? BadRequest($"Failed to delete staff with id {id}.") : NoContent();
    }
}
