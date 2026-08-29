using Application.DTOs.Season.Request;
using Application.DTOs.Season.Response;

using AutoMapper;

using Domain.Entities.Models;

namespace API.AutoMapperProfiles;

/// <summary>
/// AutoMapper profile for season ("Temporada") mappings.
/// </summary>
public class SeasonProfile : Profile
{
    /// <summary>
    /// Initializes mapping configuration for season entities.
    /// </summary>
    public SeasonProfile()
    {
        _ = CreateMap<Season, SeasonResponse>();

        _ = CreateMap<Tournament, SeasonTournamentResponse>();

        _ = CreateMap<CreateSeasonRequest, Season>();

        _ = CreateMap<UpdateSeasonRequest, Season>();
    }
}
