using Application.DTOs.Divisions.Response;
using Application.DTOs.PointDeductions.Response;
using Application.Utils.Helper.Standings;

using AutoMapper;

using Domain.Entities.Models;

namespace API.AutoMapperProfiles;

/// <summary>
/// AutoMapper profile for division-standings position mappings.
/// </summary>
public class PositionProfile : Profile
{
    /// <summary>
    /// Initializes mapping configuration for position entities.
    /// </summary>
    public PositionProfile()
    {
        _ = CreateMap<Position, PositionResponse>();

        // Disciplinary point-deduction note shown on a position row.
        _ = CreateMap<AppliedPointDeduction, AppliedPointDeductionResponse>();

        // Per-group standings for a multi-group cross-division cup (HU-110).
        // The nested Position -> PositionResponse mapping above is applied to
        // each group's Positions collection automatically.
        _ = CreateMap<GroupStandings, GroupStandingsResponse>();
    }
}
