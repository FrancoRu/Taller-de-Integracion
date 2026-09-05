using Application.DTOs.Venue.Request;
using Application.DTOs.Venue.Response;

using AutoMapper;

using Domain.Entities.Models;

namespace API.AutoMapperProfiles;

public class VenueProfile : Profile
{
    public VenueProfile()
    {
        _ = CreateMap<Venue, VenueResponse>()
            .ReverseMap();

        _ = CreateMap<CreateVenueRequest, Venue>();

        _ = CreateMap<UpdateVenueRequest, Venue>();
    }
}
