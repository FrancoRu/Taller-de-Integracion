using Application.DTOs.TeamStaff.Request;
using Application.DTOs.TeamStaff.Response;

using AutoMapper;

using Domain.Entities.Models;

namespace API.AutoMapperProfiles;

/// <summary>
/// AutoMapper profile for a team's technical staff (cuerpo técnico).
/// </summary>
public class TeamStaffProfile : Profile
{
    /// <summary>
    /// Initializes mapping configuration for team-staff entities.
    /// </summary>
    public TeamStaffProfile()
    {
        _ = CreateMap<CreateTeamStaffRequest, TeamStaff>();

        _ = CreateMap<TeamStaff, TeamStaffResponse>()
            .ForMember(
                dest => dest.Role,
                opt => opt.MapFrom(src => src.Role.ToString()))
            .ForMember(
                dest => dest.TeamName,
                opt => opt.MapFrom(src => src.Team != null ? src.Team.Name : null));
    }
}
