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
            // SeasonId flows by name convention; SeasonName is resolved from the Season navigation, degrading to null when the tournament is ungrouped or the season was not included.
            .ForMember(dest => dest.SeasonName, opt => opt.MapFrom(src => src.Season != null ? src.Season.Name : null))
            .ForMember(dest => dest.SeasonSlug, opt => opt.MapFrom(src => src.Season != null ? src.Season.Slug : null))
            .ReverseMap();

        // Tournament cloning (HU-cloning): the structure-tree read is additive and
        // maps by name convention only — Divisions resolves to DivisionStructureResponse
        // once DivisionProfile registers that map, carrying no instance data.
        _ = CreateMap<Tournament, TournamentStructureResponse>();

        _ = CreateMap<CreateTournamentRequest, Tournament>();

        _ = CreateMap<UpdateTournamentRequest, Tournament>()
            // Status is intentionally not mapped here since a status change is a guarded state-machine transition with fixture side effects driven through TournamentService.ChangeStatusAsync, never a blind field overwrite from a generic update.
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            // Category is not mapped on update since a tournament's gender category is fixed at creation; flipping it later would silently mix it with divisions already created under the original category.
            .ForMember(dest => dest.Category, opt => opt.Ignore())
            // StartDate is fixed at creation since it drives when the tournament is understood to have happened for season grouping and champions history, so it must never move after the fact regardless of status.
            .ForMember(dest => dest.StartDate, opt => opt.Ignore());
    }
}
