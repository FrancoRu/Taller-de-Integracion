using Application.DTOs.Match.Request;
using Application.DTOs.Match.Response;

using AutoMapper;

using Domain.Entities.Models;

using System;

namespace API.AutoMapperProfiles;

/// <summary>
/// AutoMapper profile for match mappings.
/// </summary>
public class MatchProfile : Profile
{
    /// <summary>
    /// Initializes mapping configuration for match entities.
    /// </summary>
    public MatchProfile()
    {
        _ = CreateMap<CreateMatchRequest, Match>();

        _ = CreateMap<Match, DetailedMatchResponse>()
            .ForMember(dest => dest.MatchType, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.HomeTeam, opt => opt.MapFrom(src => src.HomeTeam))
            .ForMember(dest => dest.VisitorTeam, opt => opt.MapFrom(src => src.VisitorTeam))
            .ForPath(dest => dest.HomeTeam!.Score, opt => opt.MapFrom(src => src.HomeScore))
            .ForPath(dest => dest.VisitorTeam!.Score, opt => opt.MapFrom(src => src.VisitorScore))
            .ForMember(dest => dest.WinningTeamName, opt => opt.MapFrom(src => src.WinningTeam != null ? src.WinningTeam.Name : null))
            .ForMember(dest => dest.WinningTeamId, opt => opt.MapFrom(src => src.WinningTeam != null ? src.WinningTeam.Id : (Guid?) null));

        _ = CreateMap<Match, MinimalMatchResponse>()
            .ForMember(dest => dest.MatchType, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.HomeTeamName, opt => opt.MapFrom(src => src.HomeTeam != null ? src.HomeTeam.Name : null))
            .ForMember(dest => dest.VisitorTeamName, opt => opt.MapFrom(src => src.VisitorTeam != null ? src.VisitorTeam.Name : null))
            .ForMember(dest => dest.WinningTeamName, opt => opt.MapFrom(src => src.WinningTeam != null ? src.WinningTeam.Name : null));

        _ = CreateMap<UpdateMatchRequest, Match>();
    }
}
