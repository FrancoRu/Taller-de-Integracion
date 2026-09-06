using Application.DTOs.Stage.Request;
using Application.DTOs.Stage.Response;
using Application.DTOs.Tournament.Response;

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

        // Tournament cloning (HU-cloning): additive, name-convention mapping —
        // carries no dates, no DrawnAt, no match data.
        _ = CreateMap<Stage, StageStructureResponse>();
    }
}
