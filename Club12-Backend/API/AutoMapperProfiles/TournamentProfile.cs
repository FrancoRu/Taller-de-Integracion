using Application.DTOs.Tournament.Request;
using Application.DTOs.Tournament.Response;

using AutoMapper;

using Domain.Entities.Models;

namespace API.AutoMapperProfiles;

public class TournamentProfile : Profile
{
    public TournamentProfile()
    {
        _ = CreateMap<Tournament, TournamentResponse>()
            .ForMember(dest => dest.Divisions, opt => opt.MapFrom(src => src.Divisions))
            // SeasonId flows by name convention; SeasonName is resolved from the
            // (optionally loaded) Season navigation, degrading to null when the
            // tournament is ungrouped or the season was not included.
            .ForMember(dest => dest.SeasonName, opt => opt.MapFrom(src => src.Season != null ? src.Season.Name : null))
            .ForMember(dest => dest.SeasonSlug, opt => opt.MapFrom(src => src.Season != null ? src.Season.Slug : null))
            .ReverseMap();

        _ = CreateMap<CreateTournamentRequest, Tournament>();

        // Status is intentionally NOT mapped here: a status change is a
        // guarded state-machine transition (with fixture side effects), driven
        // through TournamentService.ChangeStatusAsync — never a blind field
        // overwrite from a generic update. The generic update only touches the
        // tournament's descriptive fields.
        //
        // Category (HU-48) is likewise NOT mapped on update: a tournament's
        // gender category is fixed at creation ("femenino as a separate
        // tournament, by design"). Flipping it later would silently mix it
        // with the divisions already created under the original category.
        //
        // StartDate is fixed at creation too: the frontend edit form never
        // exposes it (always disabled), but that alone is not a real guard — a
        // direct API call could still slip a different date through. It drives
        // when the tournament is understood to have happened (season grouping,
        // champions history, calendar), so it must never move after the fact,
        // regardless of the tournament's status.
        _ = CreateMap<UpdateTournamentRequest, Tournament>()
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.Category, opt => opt.Ignore())
            .ForMember(dest => dest.StartDate, opt => opt.Ignore());
    }
}
