using Application.DTOs.Divisions.Response;
using Application.DTOs.PointDeductions.Response;
using Application.Utils.Helper.Standings;

using AutoMapper;

using Domain.Entities.Models;

namespace API.AutoMapperProfiles;

public class PositionProfile : Profile
{
    public PositionProfile()
    {
        _ = CreateMap<Position, PositionResponse>();

        _ = CreateMap<AppliedPointDeduction, AppliedPointDeductionResponse>();

        // The nested Position to PositionResponse mapping above is applied automatically to each group's Positions collection.
        _ = CreateMap<GroupStandings, GroupStandingsResponse>();
    }
}
