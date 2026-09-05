using Application.DTOs.Match.Response;
using Application.DTOs.MatchSeries.Response;

using AutoMapper;

using Domain.Entities.Models;

using System.Linq;

namespace API.AutoMapperProfiles;

public class MatchSeriesProfile : Profile
{
    public MatchSeriesProfile()
    {
        _ = CreateMap<MatchSeries, MatchSeriesResponse>()
            .ForMember(dest => dest.HomeTeamName, opt => opt.MapFrom(src => src.HomeTeam != null ? src.HomeTeam.Name : string.Empty))
            .ForMember(dest => dest.VisitorTeamName, opt => opt.MapFrom(src => src.VisitorTeam != null ? src.VisitorTeam.Name : string.Empty))
            .ForMember(dest => dest.WinningTeamName, opt => opt.MapFrom(src => src.WinningTeam != null ? src.WinningTeam.Name : null))
            .ForMember(dest => dest.Games, opt => opt.MapFrom(src => src.Matches.OrderBy(m => m.GameNumber)));

        _ = CreateMap<Match, SeriesGameResponse>()
            .IncludeBase<Match, MinimalMatchResponse>()
            .ForMember(dest => dest.GameNumber, opt => opt.MapFrom(src => src.GameNumber ?? 0));
    }
}
