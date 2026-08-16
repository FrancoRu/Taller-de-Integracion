using Application.DTOs.Venue.Request;
using Application.DTOs.Venue.Response;

using AutoMapper;

using Domain.Entities.Models;

namespace API.AutoMapperProfiles;

/// <summary>
/// AutoMapper profile for venue mappings.
/// </summary>
public class VenueProfile : Profile
{
    /// <summary>
    /// Initializes mapping configuration for venue entities.
    /// </summary>
    public VenueProfile()
    {
        _ = CreateMap<Venue, VenueResponse>()
            .ReverseMap();

        _ = CreateMap<CreateVenueRequest, Venue>();

        _ = CreateMap<UpdateVenueRequest, Venue>();
    }
}
