using Application.DTOs.Divisions.Response;

using AutoMapper;

using Domain.Entities.Models;

namespace API.AutoMapperProfiles;

/// <summary>
/// AutoMapper profile for division-standings position mappings.
/// </summary>
public class PositionProfile : Profile
{
    /// <summary>
    /// Initializes mapping configuration for position entities.
    /// </summary>
    public PositionProfile()
    {
        _ = CreateMap<Position, PositionResponse>();
    }
}
