using Application.DTOs.Stage.Request;
using Application.DTOs.Stage.Response;

using AutoMapper;

using Domain.Entities.Models;

namespace API.AutoMapperProfiles;

public class StageProfile : Profile
{
    public StageProfile()
    {
        _ = CreateMap<CreateStageRequest, Stage>();
        _ = CreateMap<Stage, StageResponse>()
            .ReverseMap();
        _ = CreateMap<UpdateStageRequest, Stage>();
    }
}
