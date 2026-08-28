using Application.DTOs.Tournament.Request;
using Application.DTOs.Tournament.Response;

using AutoMapper;

using Domain.Entities.Models;

namespace API.AutoMapperProfiles;

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
            .ForMember(dest => dest.Divisions, opt => opt.MapFrom(src => src.Divisions))
            .ReverseMap();

        _ = CreateMap<CreateTournamentRequest, Tournament>();

        // Status is intentionally NOT mapped here: a status change is a
        // guarded state-machine transition (with fixture side effects), driven
        // through TournamentService.ChangeStatusAsync — never a blind field
        // overwrite from a generic update. The generic update only touches the
        // tournament's descriptive fields.
        _ = CreateMap<UpdateTournamentRequest, Tournament>()
            .ForMember(dest => dest.Status, opt => opt.Ignore());
    }
}
