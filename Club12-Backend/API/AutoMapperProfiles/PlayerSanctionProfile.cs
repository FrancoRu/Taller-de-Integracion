using Application.DTOs.PlayerSanction.Request;
using Application.DTOs.PlayerSanction.Response;

using AutoMapper;

using Domain.Entities.Models;

namespace API.AutoMapperProfiles;

/// <summary>
/// AutoMapper profile for player sanction mappings.
/// </summary>
public class PlayerSanctionProfile : Profile
{
    /// <summary>
    /// Initializes mapping configuration for player sanction entities.
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
