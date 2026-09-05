using Application.DTOs.PointDeductions.Request;
using Application.DTOs.PointDeductions.Response;

using AutoMapper;

using Domain.Entities.Models;

namespace API.AutoMapperProfiles;

public class PointDeductionProfile : Profile
{
    public PointDeductionProfile()
    {
        _ = CreateMap<CreatePointDeductionRequest, TeamPointDeduction>();

        _ = CreateMap<TeamPointDeduction, PointDeductionResponse>()
            .ForMember(
                dest => dest.TeamName,
                opt => opt.MapFrom(src => src.Team != null ? src.Team.Name : null));
    }
}
