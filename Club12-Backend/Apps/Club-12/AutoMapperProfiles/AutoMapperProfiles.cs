using AutoMapper;
using Club12.Entities.DivisionEntity;
using Club12.Entities.PlayerEntity;
using Club12.Entities.TeamEntity;
using Club12.Services.DTOs.Division;
using Club12.Services.DTOs.Player;
using Club12.Services.DTOs.Team;

namespace Club12.AutoMapperProfiles;

/// <summary>
/// Mapping profiles for AutoMapper.
/// </summary>
public class Club12MapperProfile : Profile
{
    /// <summary>
    /// All the mapping profiles and definitions.
    /// </summary>
    public Club12MapperProfile()
    {
        _ = CreateMap<Team, TeamResponse>()
            .ReverseMap();

        _ = CreateMap<CreateTeamRequest, Team>();

        _ = CreateMap<Division, DivisionResponse>()
            .ReverseMap();

        _ = CreateMap<DivisionRequest, Division>();

        _ = CreateMap<Player, PlayerResponse>()
            .ReverseMap();

        _ = CreateMap<CreatePlayerRequest, Player>();
    }
}
