using AutoMapper;

using Entities.DTOs.Venue;
using Entities.Models.VenueEntity;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Services.Services.VenueService;

namespace Club12.API.Controllers;

/// <summary>
/// Controller for managing Venues.
/// </summary>
/// <param name="_venueService">The Venue service.</param>
/// <param name="_mapper">The AutoMapper instance.</param>
[Authorize(Roles = "SuperAdmin")]
[Route("api/venues/")]
[ApiController]
public class VenueController(IVenueService _venueService, IMapper _mapper) : ControllerBase
{

    /// <summary>
    /// Creates a new venue asynchronously.
    /// </summary>
    /// <param name="venueRequest">The venue creation request.</param>
    /// <returns>The created Venue response.
    /// <para>Returns 201 (Created) with the Venue response if the creation was successful.</para>
    /// <para>Returns 403 (Forbidden) if the user is not authorized.</para>
    /// </returns>
    [HttpPost()]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(VenueResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<VenueResponse>> CreateVenue(CreateVenueRequest venueRequest)
    {
        Venue mappedVenue = _mapper.Map<Venue>(venueRequest);
        Venue createdVenue = await _venueService.CreateVenueAsync(mappedVenue);
        VenueResponse venueResponse = _mapper.Map<VenueResponse>(createdVenue);

        return new ObjectResult(venueResponse) { StatusCode = StatusCodes.Status201Created };
    }

    /// <summary>
    /// Retrieves a venue by its id asynchronously.
    /// </summary>
    /// <param name="id">The id of the venue to retrieve.</param>
    /// <returns>The Venue with the specified id.
    /// <para>Returns 200 (OK) with the Venue response if it was found.</para>
    /// <para>Returns 400 (Bad Request) if the Venue with the provided id was not found.</para>
    /// </returns>
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VenueResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VenueResponse>> GetVenueById(Guid id)
    {
        Venue? venue = await _venueService.GetVenueByIdAsync(id);

        if (venue is null)
        {
            return BadRequest($"Venue with id {id} not found.");
        }

        VenueResponse venueResponse = _mapper.Map<VenueResponse>(venue);
        return Ok(venueResponse);
    }

    /// <summary>
    /// Updates a venue by its id asynchronously.
    /// </summary>
    /// <param name="venueId">The id of the venue to update.</param>
    /// <param name="venueRequest">The venue update request.</param>
    /// <returns>
    /// Returns 200 (OK) with the updated Venue response if the update was successful.
    /// Returns 400 (Bad Request) if the Venue with the provided id was not found.
    /// Returns 403 (Forbidden) if the user is not authorized.
    /// </returns>
    [HttpPut("{venueId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> UpdateVenue(Guid venueId, UpdateVenueRequest venueRequest)
    {
        Venue? existingVenue = await _venueService.GetVenueByIdAsync(venueId);

        if (existingVenue is null)
        {
            return BadRequest($"Venue with id {venueId} not found.");
        }

        _mapper.Map(venueRequest, existingVenue);
        bool updateResult = await _venueService.UpdateVenueAsync(existingVenue);

        return !updateResult ? BadRequest("Failed to update the venue.") : NoContent();
    }

    /// <summary>
    /// Deletes a venue by its id asynchronously.
    /// </summary>
    /// <param name="id">The id of the Venue to delete.</param>
    /// <returns>
    /// Returns 200 (OK) if the Venue was successfully deleted.
    /// Returns 400 (Bad Request) if the Venue with the provided id was not found.
    /// Returns 403 (Forbidden) if the user is not authorized.
    /// </returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteVenueById(Guid id)
    {
        Venue? venue = await _venueService.GetVenueByIdAsync(id);

        if (venue is null)
        {
            return BadRequest($"Venue with id {id} not found.");
        }

        bool deleteResult = await _venueService.DeleteVenueAsync(venue);
        return !deleteResult ? BadRequest($"Failed to delete the venue with id {id}.") : NoContent();
    }

    /// <summary>
    /// Retrieves all venues asynchronously.
    /// </summary>
    /// <returns>A list of all Venue responses.</returns>
    [AllowAnonymous]
    [HttpGet()]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<VenueResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<VenueResponse>>> GetAllVenues()
    {
        IEnumerable<Venue> venues = await _venueService.GetAllVenuesAsync();
        IEnumerable<VenueResponse> response = _mapper.Map<IEnumerable<VenueResponse>>(venues);

        return Ok(response);
    }
}
