using API.Utils;

using Application.DTOs.Venue.Request;
using Application.DTOs.Venue.Response;
using Application.Interfaces.Services;
using Application.Utils.Constants;

using AutoMapper;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Storage;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Controllers;

/// <summary>
/// Controller for managing Venues. Reads are public; writes require Owner
/// or Admin.
/// </summary>
/// <param name="venueService">The Venue service.</param>
/// <param name="supabaseHelper">The Supabase helper for storage operations.</param>
/// <param name="mapper">The AutoMapper instance.</param>
[Route("api/venues/")]
[ApiController]
[Authorize(Roles = Roles.AdminOrOwner)]
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
    public async Task<ActionResult<VenueResponse>> CreateVenue([FromForm] CreateVenueRequest venueRequest)
    {
        Venue venue = mapper.Map<Venue>(venueRequest);

        // A venue photo is optional: only upload and set it when one was sent.
        if (venueRequest.ImageFile is not null)
        {
            venue.PhotoUrl = await supabaseHelper.UploadImageAsync<Venue>(
                venueRequest.ImageFile.OpenReadStream(),
                venueRequest.ImageFile.FileName);
        }

        await venueService.CreateVenueAsync(venue);
        VenueResponse venueResponse = mapper.Map<VenueResponse>(venue);
        return CreatedAtAction(nameof(GetVenueById), new { idOrSlug = venueResponse.Id }, venueResponse);
    }

    /// <summary>
    /// Retrieves a venue by its id or its public slug asynchronously.
    /// </summary>
    /// <param name="idOrSlug">The id (GUID) or slug of the venue to retrieve.</param>
    /// <returns>The Venue with the specified id or slug.
    /// <para>Returns 200 (OK) with the Venue response if it was found.</para>
    /// <para>Returns 404 (Not Found) if the Venue with the provided id or slug was not found.</para>
    /// </returns>
    [AllowAnonymous]
    [HttpGet("{idOrSlug}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VenueResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VenueResponse>> GetVenueById(string idOrSlug)
    {
        Venue? venue = await venueService.GetVenueByIdOrSlugAsync(idOrSlug);

        if (venue is null)
        {
            return this.NotFoundProblem(nameof(Venue), idOrSlug);
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
    /// Updates the photo of a venue.
    /// </summary>
    /// <param name="id">The id of the venue to update the photo.</param>
    /// <param name="photoRequest">The update venue photo request.</param>
    /// <returns>Returns 200 (OK) if the photo was successfully updated.</returns>
    [HttpPut("{id:guid}/photo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateVenuePhoto(Guid id, [FromForm] UpdateVenuePhotoRequest photoRequest)
    {
        if (!photoRequest.ImageFile.IsValidImageFile())
        {
            return BadRequest(ErrorMessages.Media.InvalidImageFile);
        }

        Venue? venue = await venueService.GetVenueByIdAsync(id);
        if (venue is null)
        {
            return this.NotFoundProblem(nameof(Venue), id);
        }

        venue.PhotoUrl = await supabaseHelper.UploadImageAsync<Venue>(
            photoRequest.ImageFile.OpenReadStream(),
            photoRequest.ImageFile.FileName);

        await venueService.UpdateVenueAsync(venue);
        return Ok();
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
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteVenueById(Guid id)
    {
        Venue? venue = await venueService.GetVenueByIdAsync(id);

        if (venue is null)
        {
            return this.NotFoundProblem(nameof(Venue), id);
        }

        // Run the integrity guard first (throws 409 when the venue is still
        // referenced by matches) so the stored photo is only removed once the
        // venue row is actually gone.
        await venueService.DeleteVenueAsync(id);

        if (Uri.TryCreate(venue.PhotoUrl, UriKind.Absolute, out Uri? photoUri)
            && (photoUri.Scheme == Uri.UriSchemeHttp || photoUri.Scheme == Uri.UriSchemeHttps))
        {
            await supabaseHelper.DeleteImageAsync<Venue>(photoUri.Segments[^1]);
        }

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
