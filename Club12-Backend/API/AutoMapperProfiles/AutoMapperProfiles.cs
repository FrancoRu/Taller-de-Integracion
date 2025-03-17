using AutoMapper;

using Entities.DTOs.Abstract;
using Entities.DTOs.BlogPost;
using Entities.DTOs.Division;
using Entities.DTOs.Match;
using Entities.DTOs.Player;
using Entities.DTOs.PlayerStatistic;
using Entities.DTOs.Scorer;
using Entities.DTOs.Team;
using Entities.DTOs.TopScorer;
using Entities.DTOs.Tournament;
using Entities.DTOs.Venue;
using Entities.Models.BlogPostEntity;
using Entities.Models.DivisionEntity;
using Entities.Models.MatchEntity;
using Entities.Models.PlayerEntity;
using Entities.Models.PlayerStatisticEntity;
using Entities.Models.PositionModel;
using Entities.Models.ScorerModel;
using Entities.Models.TeamEntity;
using Entities.Models.TopScorerModel;
using Entities.Models.TournamentEntity;
using Entities.Models.VenueEntity;

namespace Club12.API.AutoMapperProfiles;

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
        _ = CreateMap<Division, DetailedDivisionResponse>()
            .ForMember(dest => dest.MatchesByWeek, opt => opt.MapFrom<MatchesByWeekResolver>())
            .ReverseMap();

        _ = CreateMap<Division, MinimalDivisionResponse>()
            .ReverseMap();

        _ = CreateMap<CreateDivisionRequest, Division>();

        _ = CreateMap<UpdateDivisionRequest, Division>();
    }
}

/// <summary>
/// AutoMapper profile for mapping between TopScorer service model and TopScorerResponse API model.
/// </summary>
public class TopScorerProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TopScorerProfile"/> class.
    /// Configures the mappings between TopScorer and TopScorerResponse.
    /// </summary>
    public TopScorerProfile()
    {
        CreateMap<TopScorer, TopScorerResponse>();
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
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.LastName.ToUpper()}, {src.Names}"))
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
            .ForPath(dest => dest.HomeTeam.Score, opt => opt.MapFrom(src => src.HomeScore))
            .ForPath(dest => dest.VisitorTeam.Score, opt => opt.MapFrom(src => src.VisitorScore))
            .ForPath(dest => dest.HomeTeam.Scorers, opt => opt.MapFrom(src => src.HomeScorers))
            .ForPath(dest => dest.VisitorTeam.Scorers, opt => opt.MapFrom(src => src.VisitorScorers))
            .ForMember(dest => dest.WinningTeamName, opt => opt.MapFrom(src => src.WinningTeam != null ? src.WinningTeam.Name : null))
            .ReverseMap();

        _ = CreateMap<Match, MinimalMatchResponse>()
            .ForMember(dest => dest.MatchType, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.HomeTeamName, opt => opt.MapFrom(src => src.HomeTeam.Name))
            .ForMember(dest => dest.VisitorTeamName, opt => opt.MapFrom(src => src.VisitorTeam.Name))
            .ForMember(dest => dest.WinningTeamName, opt => opt.MapFrom(src => src.WinningTeam != null ? src.WinningTeam.Name : null))
            .ReverseMap();

        _ = CreateMap<UpdateMatchScoreRequest, Match>()
            .ForMember(dest => dest.IsFinished, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.WinningTeam, opt => opt.MapFrom((src, dest) =>
                src.HomeScore > src.VisitorScore ? dest.HomeTeam : dest.VisitorTeam));

        _ = CreateMap<UpdateMatchRequest, Match>();
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
            .ReverseMap();

        _ = CreateMap<UpdatePlayerStatisticRequest, PlayerStatistic>();
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
        _ = CreateMap<Scorer, ScorerResponse>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.LastName.ToUpper()}, {src.Names}"));
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

/// <summary>
/// Custom resolver to map matches grouped by week into MatchesByWeek.
/// </summary>
public class MatchesByWeekResolver : IValueResolver<Division, DetailedDivisionResponse, IDictionary<int, IEnumerable<MinimalMatchResponse>>>
{
    /// <summary>
    /// Resolves the matches grouped by week into a dictionary.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <param name="destMember"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public IDictionary<int, IEnumerable<MinimalMatchResponse>> Resolve(
        Division source,
        DetailedDivisionResponse destination,
        IDictionary<int, IEnumerable<MinimalMatchResponse>> destMember,
        ResolutionContext context) => source.Matches
            .GroupBy(match => match.MatchWeek!.Value)
            .ToDictionary(
                group => group.Key,
                group => context.Mapper.Map<IEnumerable<MinimalMatchResponse>>(group.ToList())
            );
}
