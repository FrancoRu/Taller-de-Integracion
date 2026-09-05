using Application.DTOs.PlayerStatistic.Request;
using Application.DTOs.PlayerStatistic.Response;

using AutoMapper;

using Domain.Entities.Models;

using System;

namespace API.AutoMapperProfiles;

public class PlayerStatisticProfile : Profile
{
    public PlayerStatisticProfile()
    {
        _ = CreateMap<CreatePlayerStatisticRequest, PlayerStatistic>();

        _ = CreateMap<PlayerStatistic, PlayerStatisticResponse>()
            .ForMember(dest => dest.MatchDate, opt => opt.MapFrom(src => src.Match != null ? (DateTime?) src.Match.MatchDate : null))
            .ReverseMap();

        _ = CreateMap<UpdatePlayerStatisticRequest, PlayerStatistic>();
    }
}
