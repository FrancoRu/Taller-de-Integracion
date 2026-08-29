using Application.DTOs.Match.Request;
using Application.DTOs.Match.Response;
using Application.DTOs.Scorer.Response;

using AutoMapper;

using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Linq;

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
            .ForMember(dest => dest.WinningTeamId, opt => opt.MapFrom(src => src.WinningTeam != null ? src.WinningTeam.Id : (Guid?) null))
            // Attribute each of the match's scorers to its team via the player's
            // TeamId (Scorer has no TeamId) and aggregate per player, so the match
            // page can list "goleadores del partido" per side. Requires the match
            // to be loaded with Scorers.Player (see IMatchRepository).
            .AfterMap((src, dest) =>
            {
                if (dest.HomeTeam is not null)
                {
                    dest.HomeTeam.Scorers = ScorersForTeam(src, src.HomeTeamId);
                }

                if (dest.VisitorTeam is not null)
                {
                    dest.VisitorTeam.Scorers = ScorersForTeam(src, src.VisitorTeamId);
                }
            });

        _ = CreateMap<Match, MinimalMatchResponse>()
            .ForMember(dest => dest.MatchType, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.HomeTeamName, opt => opt.MapFrom(src => src.HomeTeam != null ? src.HomeTeam.Name : null))
            .ForMember(dest => dest.VisitorTeamName, opt => opt.MapFrom(src => src.VisitorTeam != null ? src.VisitorTeam.Name : null))
            .ForMember(dest => dest.WinningTeamName, opt => opt.MapFrom(src => src.WinningTeam != null ? src.WinningTeam.Name : null));

        _ = CreateMap<UpdateMatchRequest, Match>();
    }

    /// <summary>
    /// Builds the per-player scorer ranking for one team from a match's scorers,
    /// attributing a scorer to the team via <see cref="Player.TeamId"/> and
    /// summing points per player, highest first.
    /// </summary>
    private static List<ScorerByPlayerResponse> ScorersForTeam(Match match, Guid? teamId)
    {
        if (teamId is null)
        {
            return [];
        }

        return [.. match.Scorers
            .Where(scorer => scorer.Player is not null && scorer.Player.TeamId == teamId.Value)
            .GroupBy(scorer => scorer.PlayerId)
            .Select(group => new ScorerByPlayerResponse
            {
                PlayerId = group.Key,
                FullName = group.First().Player!.FullName,
                Points = group.Sum(scorer => scorer.Points),
            })
            .OrderByDescending(scorer => scorer.Points)];
    }
}
