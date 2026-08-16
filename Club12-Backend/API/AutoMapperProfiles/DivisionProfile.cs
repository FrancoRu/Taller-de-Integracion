using Application.DTOs.Divisions.Request;
using Application.DTOs.Divisions.Response;

using AutoMapper;

using Domain.Entities.Models;

namespace API.AutoMapperProfiles;

/// <summary>
/// AutoMapper profile for division mappings.
/// </summary>
public class DivisionProfile : Profile
{
    /// <summary>
    /// Initializes mapping configuration for division entities.
    /// </summary>
    public DivisionProfile()
    {
        _ = CreateMap<Division, DivisionResponse>()
            .ReverseMap();

        _ = CreateMap<Division, MinimalDivisionResponse>()
            .ReverseMap();

        _ = CreateMap<CreateDivisionRequest, Division>();

        _ = CreateMap<UpdateDivisionRequest, Division>();
    }
}
