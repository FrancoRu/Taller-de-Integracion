using Application.DTOs.Team.Request;
using Application.DTOs.Team.Response;

using AutoMapper;

using Domain.Entities.Models;

namespace API.AutoMapperProfiles;

public class TeamProfile : Profile
{
    public TeamProfile()
    {
        _ = CreateMap<Team, TeamResponse>()
            .ForMember(dest => dest.ClubId, opt => opt.MapFrom(src => src.ClubId))
            .ForMember(dest => dest.TournamentName, opt => opt.MapFrom(src => src.Tournament != null ? src.Tournament.Name : null))
            .ReverseMap();

        _ = CreateMap<Team, TeamDetailedMatchResponse>()
            .ReverseMap();

        _ = CreateMap<CreateTeamRequest, Team>()
            .ForMember(dest => dest.ThreeLetterCode, opt => opt.MapFrom(src => src.ThreeLetterCode.ToUpper()));

        _ = CreateMap<UpdateTeamRequest, Team>()
            .ForMember(dest => dest.ThreeLetterCode, opt => opt.MapFrom(src => src.ThreeLetterCode != null ? src.ThreeLetterCode.ToUpper() : null))
            .ForMember(dest => dest.JerseyStyle, opt => opt.Condition(src => src.JerseyStyle != null))
            .ForMember(dest => dest.ShirtSecondaryColor, opt => opt.Condition(src => src.ShirtSecondaryColor != null))
            .ForMember(dest => dest.ShirtTertiaryColor, opt => opt.Condition(src => src.ShirtTertiaryColor != null));
    }
}
