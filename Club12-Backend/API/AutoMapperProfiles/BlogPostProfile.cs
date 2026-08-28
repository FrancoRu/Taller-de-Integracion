using Application.DTOs.BlogPosts.Request;
using Application.DTOs.BlogPosts.Response;

using AutoMapper;

using Domain.Entities.Models;

namespace API.AutoMapperProfiles;

/// <summary>
/// AutoMapper profile for blog post mappings.
/// </summary>
public class BlogPostProfile : Profile
{
    /// <summary>
    /// Initializes mapping configuration for blog post entities.
    /// </summary>
    public BlogPostProfile()
    {
        _ = CreateMap<CreateBlogPostRequest, BlogPost>();

        _ = CreateMap<BlogPost, BlogPostResponse>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.DateCreated))
            .ReverseMap();

        _ = CreateMap<UpdateBlogPostRequest, BlogPost>()
            // Null IsPublished on the request means "leave unchanged" (HU-16):
            // only overwrite the entity's flag when the caller sent a value.
            .ForMember(dest => dest.IsPublished, opt =>
            {
                opt.PreCondition(src => src.IsPublished.HasValue);
                opt.MapFrom(src => src.IsPublished!.Value);
            });
    }
}
