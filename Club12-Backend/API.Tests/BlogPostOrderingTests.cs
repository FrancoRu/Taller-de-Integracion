using Application.DTOs.Abstract.Request;
using Application.DTOs.Abstract.Response;
using Application.DTOs.BlogPosts.Request;
using Application.Interfaces.Services;

using Domain.Entities.Models;

using Infrastructure.Persistance;

using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// Verifies the paginated blog-post list served by <c>GET /api/blog-posts</c>
/// defaults to newest-first ordering by <see cref="EntityBase.DateCreated"/>.
/// The home "Últimas noticias" section, the public <c>/blog</c> list and the
/// admin Novedades list all send no sort parameter, so the backend default is
/// the effective order — it must surface the most recently created posts first,
/// not the oldest. Each test tags its posts with a unique author so the shared
/// fixture database cannot leak rows between tests.
/// </summary>
public class BlogPostOrderingTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public BlogPostOrderingTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void FilterRequest_DefaultOrdering_IsDateCreatedDescending()
    {
        GetBlogPostsFilteredRequest request = new();

        Assert.Equal(nameof(EntityBase.DateCreated), request.OrderBy);
        Assert.Equal(SortOrder.Descending, request.Order);
    }

    [Fact]
    public async Task GetAllBlogPostsAsync_NoExplicitSort_ReturnsNewestCreatedFirst()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IBlogPostService service = scope.ServiceProvider.GetRequiredService<IBlogPostService>();

        string author = $"author-{Guid.NewGuid():N}";
        DateTime oldest = new(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        DateTime newest = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime middle = new(2026, 2, 5, 0, 0, 0, DateTimeKind.Utc);

        // Inserted out of chronological order so an insertion-order result
        // would come back oldest, newest, middle.
        db.BlogPosts.Add(NewPost(author, "Old news", oldest));
        db.BlogPosts.Add(NewPost(author, "New news", newest));
        db.BlogPosts.Add(NewPost(author, "Mid news", middle));
        await db.SaveChangesAsync();

        PaginatedResponse<BlogPost> result = await service.GetAllBlogPostsAsync(
            new GetBlogPostsFilteredRequest { Author = author, PageSize = 50 });

        List<DateTime> dates = [.. result.Items.Select(post => post.DateCreated)];

        Assert.Equal([newest, middle, oldest], dates);
        for (int i = 1; i < dates.Count; i++)
        {
            Assert.True(dates[i] <= dates[i - 1]);
        }
    }

    [Fact]
    public async Task GetAllBlogPostsAsync_ExplicitAscendingSort_OverridesDefault()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IBlogPostService service = scope.ServiceProvider.GetRequiredService<IBlogPostService>();

        string author = $"author-{Guid.NewGuid():N}";
        DateTime oldest = new(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        DateTime newest = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime middle = new(2026, 2, 5, 0, 0, 0, DateTimeKind.Utc);

        db.BlogPosts.Add(NewPost(author, "New news", newest));
        db.BlogPosts.Add(NewPost(author, "Old news", oldest));
        db.BlogPosts.Add(NewPost(author, "Mid news", middle));
        await db.SaveChangesAsync();

        PaginatedResponse<BlogPost> result = await service.GetAllBlogPostsAsync(
            new GetBlogPostsFilteredRequest
            {
                Author = author,
                PageSize = 50,
                OrderBy = nameof(EntityBase.DateCreated),
                Order = SortOrder.Ascending,
            });

        List<DateTime> dates = [.. result.Items.Select(post => post.DateCreated)];

        Assert.Equal([oldest, middle, newest], dates);
    }

    [Fact]
    public async Task GetAllBlogPostsAsync_PublicCaller_StillHidesDrafts_Regression()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IBlogPostService service = scope.ServiceProvider.GetRequiredService<IBlogPostService>();

        string author = $"author-{Guid.NewGuid():N}";
        db.BlogPosts.Add(NewPost(author, "Published", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)));
        BlogPost draft = NewPost(author, "Draft", new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc));
        draft.IsPublished = false;
        db.BlogPosts.Add(draft);
        await db.SaveChangesAsync();

        PaginatedResponse<BlogPost> result = await service.GetAllBlogPostsAsync(
            new GetBlogPostsFilteredRequest { Author = author, PageSize = 50 });

        Assert.Single(result.Items);
        Assert.All(result.Items, post => Assert.True(post.IsPublished));
    }

    private static BlogPost NewPost(string author, string title, DateTime createdAt)
    {
        return new BlogPost
        {
            Id = Guid.NewGuid(),
            CreatedBy = "test",
            DateCreated = createdAt,
            Author = author,
            Title = title,
            Slug = $"{title.ToLowerInvariant().Replace(' ', '-')}-{Guid.NewGuid():N}",
            MarkdownText = "Body.",
        };
    }
}
