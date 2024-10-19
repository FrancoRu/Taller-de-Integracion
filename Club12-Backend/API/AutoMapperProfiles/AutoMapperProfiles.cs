using AutoMapper;
using Entities.DTOs.Division;
using Entities.DTOs.Player;
using Entities.DTOs.Team;
using Entities.DTOs.Tournament;
using Entities.Models.DivisionEntity;
using Entities.Models.PlayerEntity;
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
