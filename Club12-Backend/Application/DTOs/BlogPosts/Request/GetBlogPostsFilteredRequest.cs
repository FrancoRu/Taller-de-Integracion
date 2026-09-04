using Application.DTOs.Abstract.Request;

namespace Application.DTOs.BlogPosts.Request;

/// <summary>
/// Represents a request to get filtered blog posts.
/// </summary>
public class GetBlogPostsFilteredRequest : PaginatedFilterRequest
{
    /// <summary>
    /// Defaults the list to newest-created-first. The home "Últimas noticias"
    /// section, the public <c>/blog</c> list and the admin Novedades list all
    /// call <c>GET /api/blog-posts</c> without a sort parameter and never wire
    /// server-side column sorting, so this is the effective order of the list.
    /// Overrides the base <see cref="PaginatedFilterRequest"/> default of
    /// <c>DateCreated</c> ascending; an explicit <c>orderBy</c>/<c>order</c> in
    /// the query string still wins.
    /// </summary>
    public GetBlogPostsFilteredRequest()
    {
        // String literal matches EntityBase.DateCreated; QueryableExtensions
        // .SortBy resolves this name against the entity by reflection.
        OrderBy = "DateCreated";
        Order = SortOrder.Descending;
    }

    /// <summary>
    /// The author to filter blog posts by.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// The title to filter blog posts by.
    /// </summary>
    public string? Title { get; set; }
}
