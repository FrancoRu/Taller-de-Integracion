using AutoMapper;
using Application.DTOs.Abstract.Response;
using Application.DTOs.BlogPosts.Request;
using Application.DTOs.BlogPosts.Response;
using Application.DTOs.Divisions.Request;
using Application.DTOs.Divisions.Response;
using Application.DTOs.Match.Request;
using Application.DTOs.Match.Response;
using Application.DTOs.MatchSeries.Response;
using Application.DTOs.Player.Request;
using Application.DTOs.Player.Response;
using Application.DTOs.PlayerSanction.Request;
using Application.DTOs.PlayerSanction.Response;
using Application.DTOs.PlayerStatistic.Request;
using Application.DTOs.PlayerStatistic.Response;
using Application.DTOs.Scorer.Response;
using Application.DTOs.Stage.Request;
using Application.DTOs.Stage.Response;
using Application.DTOs.Team.Request;
using Application.DTOs.Team.Response;
using Application.DTOs.TopScorer.Response;
using Application.DTOs.Tournament.Request;
using Application.DTOs.Tournament.Response;
using Application.DTOs.Venue.Request;
using Application.DTOs.Venue.Response;
using System;
using System.Collections.Generic;
using System.Linq;
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

/// <summary>
/// AutoMapper profile for division mappings.
/// </summary>
public class DivisionProfile : Profile
{
    /// <summary>
    /// Initializes mapping configuration for division entities.
    /// </summary>
    public DivisionProfile()
    {
        _ = CreateMap<Division, DivisionResponse>()
            .ReverseMap();

        _ = CreateMap<Position, PositionResponse>();

        _ = CreateMap<Division, MinimalDivisionResponse>()
            .ReverseMap();

        _ = CreateMap<CreateDivisionRequest, Division>();

        _ = CreateMap<UpdateDivisionRequest, Division>();
    }
}


/// <summary>
/// AutoMapper profile for player mappings.
/// </summary>
public class PlayerProfile : Profile
{
    /// <summary>
    /// Initializes mapping configuration for player entities.
    /// </summary>
    public PlayerProfile()
    {
        _ = CreateMap<Player, PublicPlayerResponse>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
            .ReverseMap();

        CreateMap<Player, AdminPlayerResponse>()
            .IncludeBase<Player, PublicPlayerResponse>();

        _ = CreateMap<CreatePlayerRequest, Player>();

        _ = CreateMap<UpdatePlayerRequest, Player>();
    }
}

/// <summary>
/// AutoMapper profile for tournament mappings.
/// </summary>
public class TournamentProfile : Profile
{
    /// <summary>
    /// Initializes mapping configuration for tournament entities.
    /// </summary>
    public TournamentProfile()
    {
        _ = CreateMap<Tournament, TournamentResponse>()
            .ForMember(dest => dest.Divisions, opt => opt.MapFrom(src => src.Divisions))
            .ReverseMap();

        _ = CreateMap<CreateTournamentRequest, Tournament>();

        _ = CreateMap<UpdateTournamentRequest, Tournament>();
    }
}


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
            .ForMember(dest => dest.HomeTeam, opt => opt.MapFrom(src => src.HomeTeam))
            .ForMember(dest => dest.VisitorTeam, opt => opt.MapFrom(src => src.VisitorTeam))
            .ForPath(dest => dest.HomeTeam!.Score, opt => opt.MapFrom(src => src.HomeScore))
            .ForPath(dest => dest.VisitorTeam!.Score, opt => opt.MapFrom(src => src.VisitorScore))
            .ForMember(dest => dest.WinningTeamName, opt => opt.MapFrom(src => src.WinningTeam != null ? src.WinningTeam.Name : null))
            .ForMember(dest => dest.WinningTeamId, opt => opt.MapFrom(src => src.WinningTeam != null ? src.WinningTeam.Id : (Guid?)null))
            .ReverseMap();

        _ = CreateMap<Match, MinimalMatchResponse>()
            .ForMember(dest => dest.MatchType, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.HomeTeamName, opt => opt.MapFrom(src => src.HomeTeam != null ? src.HomeTeam.Name : null))
            .ForMember(dest => dest.VisitorTeamName, opt => opt.MapFrom(src => src.VisitorTeam != null ? src.VisitorTeam.Name : null))
            .ForMember(dest => dest.WinningTeamName, opt => opt.MapFrom(src => src.WinningTeam != null ? src.WinningTeam.Name : null))
            .ReverseMap();

        _ = CreateMap<UpdateMatchScoreRequest, Match>()
            .ForMember(dest => dest.IsFinished, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.WinningTeam, opt => opt.MapFrom((src, dest) =>
                src.HomeScore > src.VisitorScore ? dest.HomeTeam : dest.VisitorTeam))
            .ForMember(dest => dest.HomeScore, opt => opt.MapFrom(src => src.HomeScore))
            .ForMember(dest => dest.VisitorScore, opt => opt.MapFrom(src => src.VisitorScore));

        _ = CreateMap<UpdateMatchRequest, Match>();
    }
}

/// <summary>
/// AutoMapper profile for best-of-N playoff series mappings.
/// </summary>
public class MatchSeriesProfile : Profile
{
    /// <summary>
    /// Initializes mapping configuration for series entities.
    /// </summary>
    public MatchSeriesProfile()
    {
        _ = CreateMap<Domain.Entities.Models.MatchSeries, MatchSeriesResponse>()
            .ForMember(dest => dest.HomeTeamName, opt => opt.MapFrom(src => src.HomeTeam != null ? src.HomeTeam.Name : string.Empty))
            .ForMember(dest => dest.VisitorTeamName, opt => opt.MapFrom(src => src.VisitorTeam != null ? src.VisitorTeam.Name : string.Empty))
            .ForMember(dest => dest.WinningTeamName, opt => opt.MapFrom(src => src.WinningTeam != null ? src.WinningTeam.Name : null))
            .ForMember(dest => dest.Games, opt => opt.MapFrom(src => src.Matches.OrderBy(m => m.GameNumber)));

        _ = CreateMap<Match, SeriesGameResponse>()
            .IncludeBase<Match, MinimalMatchResponse>()
            .ForMember(dest => dest.GameNumber, opt => opt.MapFrom(src => src.GameNumber ?? 0));
    }
}

/// <summary>
/// AutoMapper profile for venue mappings.
/// </summary>
public class VenueProfile : Profile
{
    /// <summary>
    /// Initializes mapping configuration for venue entities.
    /// </summary>
    public VenueProfile()
    {
        _ = CreateMap<Venue, VenueResponse>()
            .ReverseMap();

        _ = CreateMap<CreateVenueRequest, Venue>();

        _ = CreateMap<UpdateVenueRequest, Venue>();
    }
}

/// <summary>
/// AutoMapper profile for player statistics.
/// </summary>
public class PlayerStatisticProfile : Profile
{
    /// <summary>
    /// Initializes mapping configuration for player statistics.
    /// </summary>
    public PlayerStatisticProfile()
    {
        _ = CreateMap<CreatePlayerStatisticRequest, PlayerStatistic>();

        _ = CreateMap<PlayerStatistic, PlayerStatisticResponse>()
            .ForMember(dest => dest.MatchDate, opt => opt.MapFrom(src => src.Match != null ? (DateTime?)src.Match.MatchDate : null))
            .ReverseMap();

        _ = CreateMap<UpdatePlayerStatisticRequest, PlayerStatistic>();
    }
}

/// <summary>
/// AutoMapper profile for player sanction.
/// </summary>
public class PlayerSanctionProfile : Profile
{
    /// <summary>
    /// Initializes mapping configuration for player sanction.
    /// </summary>
    public PlayerSanctionProfile()
    {
        _ = CreateMap<CreatePlayerSanctionRequest, PlayerSanction>();
        _ = CreateMap<PlayerSanction, PlayerSanctionResponse>()
            .ForMember(dest => dest.PlayerFullName, opt => opt.MapFrom(src => src.Player.FullName))
            .ReverseMap()
            .ForMember(dest => dest.Player, opt => opt.Ignore());

        _ = CreateMap<UpdatePlayerSanctionRequest, PlayerSanction>();
    }
}
/// <summary>
/// AutoMapper profile for scorer mappings.
/// </summary>
public class ScorerProfile : Profile
{
    /// <summary>
    /// Initializes mapping configuration for scorer entities.
    /// </summary>
    public ScorerProfile()
    {
        
    }
}


/// <summary>
/// AutoMapper profile for blog post mappings.
/// </summary>
public class BlogPostProfile : Profile
{
    /// <summary>
    /// Initializes mapping configuration for blog post entities.
    /// </summary>
    public BlogPostProfile()
    {
        _ = CreateMap<CreateBlogPostRequest, BlogPost>();

        _ = CreateMap<BlogPost, BlogPostResponse>()
            .ReverseMap();

        _ = CreateMap<UpdateBlogPostRequest, BlogPost>();
    }
}

/// <summary>
/// AutoMapper profile for stage mappings.
/// </summary>
public class StageProfile: Profile
{
    /// <summary>
    /// Initializes mapping configuration for stage entities.
    /// </summary>
    public StageProfile()
    {
        _ = CreateMap<CreateStageRequest, Stage>();
        _ = CreateMap<Stage, StageResponse>()
            .ReverseMap();
        _ = CreateMap<UpdateStageRequest, Stage>();
    }
}

/// <summary>
/// AutoMapper profile for position mappings.
/// </summary>
public class PositionProfile : Profile
{
    /// <summary>
    /// Initializes the mapping configuration for PositionDTO and PositionResponse.
    /// </summary>
    public PositionProfile()
    {
        _ = CreateMap<Position, PositionResponse>();
    }
}

/// <summary>
/// AutoMapper profile for paginated response mappings.
/// </summary>
public class PaginatedResponseProfile : Profile
{
    /// <summary>
    /// Initializes mapping configuration for paginated responses.
    /// </summary>
    public PaginatedResponseProfile()
    {
        CreateMap(typeof(PaginatedResponse<>), typeof(PaginatedResponse<>))
            .ConvertUsing(typeof(PaginatedResponseConverter<,>));
    }
}

/// <summary>
/// Converter to handle mapping between paginated responses with different types.
/// </summary>
/// <typeparam name="TSource">The source entity type.</typeparam>
/// <typeparam name="TDestination">The destination DTO type.</typeparam>
public class PaginatedResponseConverter<TSource, TDestination>
    : ITypeConverter<PaginatedResponse<TSource>, PaginatedResponse<TDestination>>
{
    /// <summary>
    /// Applies the mapping between paginated responses with different types.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public PaginatedResponse<TDestination> Convert(
        PaginatedResponse<TSource> source,
        PaginatedResponse<TDestination> destination,
        ResolutionContext context) => new()
        {
            Page = source.Page,
            PageSize = source.PageSize,
            TotalCount = source.TotalCount,
            Items = context.Mapper.Map<List<TDestination>>(source.Items)
        };
}


