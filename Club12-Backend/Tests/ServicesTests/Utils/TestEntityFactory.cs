namespace Services.Tests.Utils;

public static class TestEntityFactory
{
    // Example mock setup
    public static void SetupFilterWithPagination(
        IGenericService<BlogPost> genericService,
        IQueryable<BlogPost> blogPosts,
        GetBlogPostsFilteredRequest filter)
    {
        genericService.FilterByExpressionWithPagination(
            Arg.Any<Expression<Func<BlogPost, bool>>>(),
            filter
        ).Returns(blogPosts);

        genericService.GetCountAsync(Arg.Any<Expression<Func<BlogPost, bool>>>())
            .Returns(Task.FromResult(blogPosts.Count()));
    }

    public static IQueryable<BlogPost> CreateBlogPosts()
    {
        return new List<BlogPost>
    {
        CreateBlogPost(Guid.NewGuid(), "Author1", "Title1", "MarkdownText1"),
        CreateBlogPost(Guid.NewGuid(), "Author2", "Title2", "MarkdownText2")
    }.AsAsyncQueryable();
    }

    public static BlogPost CreateBlogPost(Guid id, string author = "Author", string title = "Title", string markdownText = "Content")
    {
        return new BlogPost
        {
            Id = id,
            Author = author,
            Title = title,
            MarkdownText = markdownText
        };
    }

    public static GetBlogPostsFilteredRequest CreateBlogPostsFilter(int pageNumber = 1, int pageSize = 10)
    {
        return new GetBlogPostsFilteredRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}