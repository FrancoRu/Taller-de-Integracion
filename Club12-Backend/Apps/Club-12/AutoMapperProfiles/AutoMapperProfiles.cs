using AutoMapper;
using Club12.Entities.DivisionEntity;
using Club12.Entities.PlayerEntity;
using Club12.Entities.TeamEntity;
using Club12.Entities.UserEntity;
using Club12.Services.Utils;
using Club12.Viewmodels.Division;
using Club12.Viewmodels.Player;
using Club12.Viewmodels.Team;
using Club12.Viewmodels.User;

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

        _ = CreateMap<TeamRequest, Team>();

        _ = CreateMap<Division, DivisionResponse>()
            .ReverseMap();

        _ = CreateMap<DivisionRequest, Division>();

        _ = CreateMap<Player, PlayerResponse>()
            .ReverseMap();

        _ = CreateMap<PlayerRequest, Player>();

        _ = CreateMap<CreateUserRequest, User>()
            .ForMember(dest => dest.Password, opt => opt.MapFrom(src => Encrypt.Hash(src.Password)))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => "Admin"));
    }
}
