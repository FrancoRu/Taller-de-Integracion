using Application.DTOs.Divisions.Request;
using Application.DTOs.Divisions.Response;

using AutoMapper;

using Domain.Entities.Models;

namespace API.AutoMapperProfiles;

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

        _ = CreateMap<Division, MinimalDivisionResponse>()
            .ReverseMap();

        _ = CreateMap<CreateDivisionRequest, Division>();

        // TournamentId is deliberately excluded from the blind convention
        // mapping: reassignment must resolve and validate the target
        // Tournament entity first (see DivisionService.TryAssignTournamentAsync),
        // otherwise a null/omitted TournamentId in the request would map
        // Guid? -> Guid via GetValueOrDefault() and silently zero out the
        // division's real tournament.
        _ = CreateMap<UpdateDivisionRequest, Division>()
            .ForMember(dest => dest.TournamentId, opt => opt.Ignore());
    }
}
