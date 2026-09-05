using Application.DTOs.Divisions.Request;
using Application.DTOs.Divisions.Response;
using Application.Utils.Helper.Playoff;

using AutoMapper;

using Domain.Entities.Models;

namespace API.AutoMapperProfiles;

public class DivisionProfile : Profile
{
    public DivisionProfile()
    {
        _ = CreateMap<Division, DivisionResponse>()
            // QualificationRanges is derived from PlayoffMappings (HU-45): the
            // same ranges re-shaped and ordered top-down for the public
            // standings highlight. It has no Division counterpart, so it is
            // computed here and ignored on the reverse map.
            .ForMember(
                dest => dest.QualificationRanges,
                opt => opt.MapFrom(src => QualificationRangeBuilder.Build(src.PlayoffMappings)))
            // TournamentSlug is resolved from the (optionally loaded) Tournament
            // navigation, degrading to null when it was not included.
            .ForMember(
                dest => dest.TournamentSlug,
                opt => opt.MapFrom(src => src.Tournament != null ? src.Tournament.Slug : null))
            .ReverseMap();

        _ = CreateMap<Division, MinimalDivisionResponse>()
            .ReverseMap();

        _ = CreateMap<PlayoffMappingRequest, DivisionPlayoffMapping>();
        _ = CreateMap<DivisionPlayoffMapping, PlayoffMappingResponse>();

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
