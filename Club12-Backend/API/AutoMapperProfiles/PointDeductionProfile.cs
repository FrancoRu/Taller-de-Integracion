using Application.DTOs.PointDeductions.Request;
using Application.DTOs.PointDeductions.Response;

using AutoMapper;

using Domain.Entities.Models;

namespace API.AutoMapperProfiles;

/// <summary>
/// AutoMapper profile for disciplinary point deductions (deducción de puntos).
/// </summary>
public class PointDeductionProfile : Profile
{
    /// <summary>
    /// Initializes mapping configuration for point-deduction entities.
    /// </summary>
    public PointDeductionProfile()
    {
        _ = CreateMap<CreatePointDeductionRequest, TeamPointDeduction>();

        _ = CreateMap<TeamPointDeduction, PointDeductionResponse>()
            .ForMember(
                dest => dest.TeamName,
                opt => opt.MapFrom(src => src.Team != null ? src.Team.Name : null));
    }
}
