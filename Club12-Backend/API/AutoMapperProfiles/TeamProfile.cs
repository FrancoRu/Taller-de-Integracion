using Application.DTOs.Team.Request;
using Application.DTOs.Team.Response;

using AutoMapper;

using Domain.Entities.Models;

namespace API.AutoMapperProfiles;

/// <summary>
/// AutoMapper profile for team mappings.
/// </summary>
public class TeamProfile : Profile
{
    /// <summary>
    /// Initializes mapping configuration for team entities.
    /// </summary>
    public TeamProfile()
    {
        _ = CreateMap<Team, TeamResponse>()
            .ReverseMap();

        _ = CreateMap<Team, TeamDetailedMatchResponse>()
            .ReverseMap();

        _ = CreateMap<CreateTeamRequest, Team>()
            .ForMember(dest => dest.ThreeLetterCode, opt => opt.MapFrom(src => src.ThreeLetterCode.ToUpper()));

        _ = CreateMap<UpdateTeamRequest, Team>()
            .ForMember(dest => dest.ThreeLetterCode, opt => opt.MapFrom(src => src.ThreeLetterCode != null ? src.ThreeLetterCode.ToUpper() : null));
    }
}
