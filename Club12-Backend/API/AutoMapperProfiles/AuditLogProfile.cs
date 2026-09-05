using Application.DTOs.AuditLogs.Response;

using AutoMapper;

using Domain.Entities.Models;

namespace API.AutoMapperProfiles;

public class AuditLogProfile : Profile
{
    public AuditLogProfile()
    {
        _ = CreateMap<AuditLog, AuditLogResponse>()
            .ForMember(dest => dest.Action, opt => opt.MapFrom(src => src.Action.ToString()))
            .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.DateCreated));
    }
}
