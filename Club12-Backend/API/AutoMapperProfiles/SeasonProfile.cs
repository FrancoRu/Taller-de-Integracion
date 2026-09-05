using Application.DTOs.Season.Request;
using Application.DTOs.Season.Response;

using AutoMapper;

using Domain.Entities.Models;

namespace API.AutoMapperProfiles;

public class SeasonProfile : Profile
{
    public SeasonProfile()
    {
        _ = CreateMap<Season, SeasonResponse>();

        _ = CreateMap<Tournament, SeasonTournamentResponse>();

        _ = CreateMap<CreateSeasonRequest, Season>();

        _ = CreateMap<UpdateSeasonRequest, Season>();
    }
}
