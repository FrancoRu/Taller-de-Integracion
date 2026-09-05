using Application.DTOs.Player.Request;
using Application.DTOs.Player.Response;

using AutoMapper;

using Domain.Entities.Models;

namespace API.AutoMapperProfiles;

public class PlayerProfile : Profile
{
    public PlayerProfile()
    {
        _ = CreateMap<Player, PublicPlayerResponse>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
            .ReverseMap();

        _ = CreateMap<Player, AdminPlayerResponse>()
            .IncludeBase<Player, PublicPlayerResponse>();

        _ = CreateMap<CreatePlayerRequest, Player>();

        _ = CreateMap<UpdatePlayerRequest, Player>();

        _ = CreateMap<PlayerTeamRegistration, PlayerRegistrationResponse>();
    }
}
