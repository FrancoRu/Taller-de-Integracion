using Application.DTOs.Abstract.Response;
using Application.DTOs.BlogPosts.Request;
using Application.Interfaces.Services;

using Domain.Entities.Models;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using System;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

namespace API.Tests;

/// <summary>
/// HU-16: public blog reads only ever expose published posts, while
/// Admin/Owner callers (modelled here by the includeUnpublished flag the
/// controller sets from the caller's role) also see drafts.
/// </summary>
public class BlogPostDraftVisibilityTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public BlogPostDraftVisibilityTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PublicListHidesDrafts_AdminSeesThem()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IBlogPostService service = scope.ServiceProvider.GetRequiredService<IBlogPostService>();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        // Unique author so the filtered listing only ever sees these two posts,
        // regardless of what other tests left in the shared database.
        string author = $"author-{Guid.NewGuid():N}";

        db.BlogPosts.Add(NewPost(author, "Published post", isPublished: true));
        db.BlogPosts.Add(NewPost(author, "Draft post", isPublished: false));
        await db.SaveChangesAsync();

        GetBlogPostsFilteredRequest filter = new() { Author = author, PageSize = 50 };

        PaginatedResponse<BlogPost> publicView = await service.GetAllBlogPostsAsync(filter);
        Assert.Single(publicView.Items);
        Assert.All(publicView.Items, post => Assert.True(post.IsPublished));

        PaginatedResponse<BlogPost> adminView = await service.GetAllBlogPostsAsync(filter, includeUnpublished: true);
        Assert.Equal(2, adminView.Items.Count());
    }

    [Fact]
    public async Task PublicDetailHidesDraft_AdminResolvesIt()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IBlogPostService service = scope.ServiceProvider.GetRequiredService<IBlogPostService>();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        string slug = $"draft-{Guid.NewGuid():N}";
        BlogPost draft = NewPost($"author-{Guid.NewGuid():N}", "Hidden draft", isPublished: false);
        draft.Slug = slug;
        db.BlogPosts.Add(draft);
        await db.SaveChangesAsync();

        // Public caller: a draft is treated as not found.
        Assert.Null(await service.GetBlogPostByIdOrSlugAsync(slug));

        // Admin/Owner caller: the draft resolves.
        BlogPost? resolved = await service.GetBlogPostByIdOrSlugAsync(slug, includeUnpublished: true);
        Assert.NotNull(resolved);
        Assert.Equal(slug, resolved!.Slug);
    }

    private static BlogPost NewPost(string author, string title, bool isPublished)
    {
        return new BlogPost
        {
            Id = Guid.NewGuid(),
            CreatedBy = "test",
            Author = author,
            Title = title,
            Slug = $"{title.ToLowerInvariant().Replace(' ', '-')}-{Guid.NewGuid():N}",
            MarkdownText = "Body.",
            IsPublished = isPublished,
        };
    }
}
