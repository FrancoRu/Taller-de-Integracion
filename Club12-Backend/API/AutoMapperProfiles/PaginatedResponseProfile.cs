using Application.DTOs.Abstract.Response;

using AutoMapper;

namespace API.AutoMapperProfiles;

/// <summary>
/// AutoMapper profile for paginated response mappings.
/// </summary>
public class PaginatedResponseProfile : Profile
{
    /// <summary>
    /// Initializes mapping configuration for paginated responses.
    /// </summary>
    public PaginatedResponseProfile()
    {
        CreateMap(typeof(PaginatedResponse<>), typeof(PaginatedResponse<>))
            .ConvertUsing(typeof(PaginatedResponseConverter<,>));
    }
}
