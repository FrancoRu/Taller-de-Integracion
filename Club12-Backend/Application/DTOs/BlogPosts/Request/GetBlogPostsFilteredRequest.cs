using Application.DTOs.Abstract.Request;

namespace Application.DTOs.BlogPosts.Request;

/// <summary>
/// Represents a request to get filtered blog posts.
/// </summary>
public class GetBlogPostsFilteredRequest : PaginatedFilterRequest
{
    /// <summary>
    /// Overrides the base PaginatedFilterRequest default to sort newest-created-first.
    /// </summary>
    public GetBlogPostsFilteredRequest()
    {
        // String literal matches EntityBase.DateCreated; QueryableExtensions.SortBy resolves this name against the entity by reflection.
        OrderBy = "DateCreated";
        Order = SortOrder.Descending;
    }

    public string? Author { get; set; }

    public string? Title { get; set; }
}
