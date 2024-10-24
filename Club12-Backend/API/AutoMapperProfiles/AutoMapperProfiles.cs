using AutoMapper;

using Entities.DTOs.Abstract;
using Entities.DTOs.Division;
using Entities.DTOs.Match;
using Entities.DTOs.Player;
using Entities.DTOs.PlayerStatistic;
using Entities.DTOs.Team;
using Entities.DTOs.Tournament;
using Entities.Models.DivisionEntity;
using Entities.Models.MatchEntity;
using Entities.Models.PlayerEntity;
using Entities.Models.PlayerStatisticEntity;
using Entities.Models.TeamEntity;
using Entities.Models.TournamentEntity;

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

        _ = CreateMap<CreateTeamRequest, Team>();

        _ = CreateMap<UpdateTeamRequest, Team>();
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
        _ = CreateMap<Player, PlayerResponse>()
                .ReverseMap();

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
        _ = CreateMap<CreateMatchRequest, Match>()
                .ForMember(dest => dest.HomeTeamId, opt => opt.MapFrom(src => src.HomeTeamId))
                .ForMember(dest => dest.VisitorTeamId, opt => opt.MapFrom(src => src.VisitorTeamId))
                .ForMember(dest => dest.DivisionId, opt => opt.MapFrom(src => src.DivisionId));

        _ = CreateMap<Match, MatchResponse>()
                .ForMember(dest => dest.HomeTeamName, opt => opt.MapFrom(src => src.HomeTeam.Name))
                .ForMember(dest => dest.VisitorTeamName, opt => opt.MapFrom(src => src.VisitorTeam.Name))
                .ForMember(dest => dest.WinningTeamName, opt => opt.MapFrom(src => src.WinningTeam != null ? src.WinningTeam.Name : null))
                .ReverseMap();

        _ = CreateMap<UpdateMatchScoreRequest, Match>()
                .ForMember(dest => dest.HomeScore, opt => opt.MapFrom(src => src.HomeScore))
                .ForMember(dest => dest.VisitorScore, opt => opt.MapFrom(src => src.VisitorScore));

        _ = CreateMap<UpdateMatchRequest, Match>();
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
        ResolutionContext context)
    {
        return new PaginatedResponse<TDestination>
        {
            Page = source.Page,
            PageSize = source.PageSize,
            TotalCount = source.TotalCount,
            Items = context.Mapper.Map<List<TDestination>>(source.Items)
        };
    }
}



