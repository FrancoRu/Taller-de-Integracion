using Application.DTOs.Abstract.Response;

using AutoMapper;

namespace API.AutoMapperProfiles;

public class PaginatedResponseProfile : Profile
{
    public PaginatedResponseProfile()
    {
        CreateMap(typeof(PaginatedResponse<>), typeof(PaginatedResponse<>))
            .ConvertUsing(typeof(PaginatedResponseConverter<,>));
    }
}
