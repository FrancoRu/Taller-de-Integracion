using Application.DTOs.PlayerSanction.Request;
using Application.DTOs.PlayerSanction.Response;

using AutoMapper;

using Domain.Entities.Models;

namespace API.AutoMapperProfiles;

public class PlayerSanctionProfile : Profile
{
    public PlayerSanctionProfile()
    {
        _ = CreateMap<CreatePlayerSanctionRequest, PlayerSanction>();
        _ = CreateMap<PlayerSanction, PlayerSanctionResponse>()
            .ForMember(dest => dest.PlayerFullName,
                opt => opt.MapFrom(src => src.Player != null ? src.Player.FullName : null))
            .ForMember(dest => dest.TeamName,
                opt => opt.MapFrom(src => src.Team != null ? src.Team.Name : null))
            // FechasRemaining and IsActive are computed against finished rounds and populated by the controller, not mapped here.
            .ForMember(dest => dest.FechasRemaining, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ReverseMap()
            .ForMember(dest => dest.Player, opt => opt.Ignore())
            .ForMember(dest => dest.Team, opt => opt.Ignore());

        _ = CreateMap<UpdatePlayerSanctionRequest, PlayerSanction>();
    }
}
