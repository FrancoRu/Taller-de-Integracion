using AutoMapper;
using API.Utils;
using Application.DTOs.Venue.Request;
using Application.DTOs.Venue.Response;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities.Models;
using System.Linq;
using Application.Utils.Helper.SupabaseHelper;

namespace API.Controllers;

/// <summary>
/// Controller for managing Venues.
/// </summary>
/// <param name="venueService">The Venue service.</param>
/// <param name="supabaseHelper">The Supabase helper for storage operations.</param>
/// <param name="mapper">The AutoMapper instance.</param>
[Route("api/venues/")]
[ApiController]
public class VenueController(IVenueService venueService, SupabaseHelper supabaseHelper, IMapper mapper) : ControllerBase
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
        string logoUrl = await supabaseHelper.UploadImageAsync<Venue>(venueRequest.ImageFile.OpenReadStream(), venueRequest.ImageFile.FileName);
        Venue venue = mapper.Map<Venue>(venueRequest);
        venue.PhotoUrl = logoUrl;
        await venueService.CreateVenueAsync(venue);
        VenueResponse venueResponse = mapper.Map<VenueResponse>(venue);
        return CreatedAtAction(nameof(GetVenueById), new { venueResponse.Id }, venueResponse);
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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VenueResponse>> GetVenueById(Guid id)
    {
        Venue? venue = await venueService.GetVenueByIdAsync(id);

        if (venue is null)
        {
            return this.NotFoundProblem(nameof(Venue), id);
        }

        VenueResponse venueResponse = mapper.Map<VenueResponse>(venue);
        return Ok(venueResponse);
    }

    /// <summary>
    /// Updates a venue by its id asynchronously.
    /// </summary>
    /// <param name="id">The id of the venue to update.</param>
    /// <param name="venueRequest">The venue update request.</param>
    /// <returns>
    /// Returns 200 (OK) with the updated Venue response if the update was successful.
    /// Returns 400 (Bad Request) if the Venue with the provided id was not found.
    /// Returns 403 (Forbidden) if the user is not authorized.
    /// </returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> UpdateVenue(Guid id, UpdateVenueRequest venueRequest)
    {
        Venue? existingVenue = await venueService.GetVenueByIdAsync(id);

        if (existingVenue is null)
        {
            return this.NotFoundProblem(nameof(Venue), id);
        }

        mapper.Map(venueRequest, existingVenue);

        await venueService.UpdateVenueAsync(existingVenue);

        return NoContent();
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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteVenueById(Guid id)
    {
        Venue? venue = await venueService.GetVenueByIdAsync(id);

        if (venue is null)
        {
            return this.NotFoundProblem(nameof(Venue), id);
        }

        if (!string.IsNullOrWhiteSpace(venue.PhotoUrl))
        {
            await supabaseHelper.DeleteImageAsync<Team>(venue.PhotoUrl.Split('/').Last());
        }

        await venueService.DeleteVenueAsync(id);
        return NoContent();
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
        IEnumerable<Venue> venues = await venueService.GetAllVenuesAsync();
        IEnumerable<VenueResponse> response = mapper.Map<IEnumerable<VenueResponse>>(venues);

        return Ok(response);
    }
}
