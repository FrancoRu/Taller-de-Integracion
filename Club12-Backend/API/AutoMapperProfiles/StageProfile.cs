using Application.DTOs.Stage.Request;
using Application.DTOs.Stage.Response;

using AutoMapper;

using Domain.Entities.Models;

namespace API.AutoMapperProfiles;

/// <summary>
/// AutoMapper profile for stage mappings.
/// </summary>
public class StageProfile : Profile
{
    /// <summary>
    /// Initializes mapping configuration for stage entities.
    /// </summary>
    public StageProfile()
    {
        _ = CreateMap<CreateStageRequest, Stage>();
        _ = CreateMap<Stage, StageResponse>()
            .ReverseMap();
        _ = CreateMap<UpdateStageRequest, Stage>();
    }
}
