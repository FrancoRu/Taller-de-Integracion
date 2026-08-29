using API.AutoMapperProfiles;
using API.Controllers;

using Application.DTOs.Abstract.Response;
using Application.DTOs.BlogPosts.Request;
using Application.DTOs.BlogPosts.Response;
using Application.Interfaces.Services;

using AutoMapper;

using Domain.Entities.Models;
using Domain.Enums;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API.Tests;

/// <summary>
/// Covers the blog-post view counter (QA wave 1, Bug 3): the counter must
/// increment exactly ONCE per genuine PUBLIC read of a post, and must NOT be
/// inflated by an Admin/Owner opening the post to preview or edit it. Exercised
/// as a direct-controller test because BlogPostController takes a
/// constructor-injected SupabaseHelper (live-connection ctor) that the
/// not-found branch and this read path never touch — the same documented host
/// testability gap as SupabaseDependentControllerNotFoundTests.
/// </summary>
public class BlogPostViewCounterTests
{
    private static readonly IMapper Mapper = new MapperConfiguration(
        cfg => cfg.AddProfile<BlogPostProfile>(), NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task GetBlogPostById_PublicView_IncrementsViewsExactlyOnce()
    {
        RecordingBlogPostService service = new(Published(views: 7));
        BlogPostController controller = WithUser(new BlogPostController(service, null!, Mapper), roles: []);

        ActionResult<BlogPostResponse> result = await controller.GetBlogPostById("some-post");

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        BlogPostResponse response = Assert.IsType<BlogPostResponse>(ok.Value);

        Assert.Equal(8, response.Views);
        Assert.Equal(1, service.UpdateCount);
        Assert.Equal(8, service.LastPersistedViews);
    }

    [Fact]
    public async Task GetBlogPostById_AdminView_DoesNotIncrementViews()
    {
        RecordingBlogPostService service = new(Published(views: 7));
        BlogPostController controller = WithUser(
            new BlogPostController(service, null!, Mapper), roles: [Roles.Admin]);

        ActionResult<BlogPostResponse> result = await controller.GetBlogPostById("some-post");

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        BlogPostResponse response = Assert.IsType<BlogPostResponse>(ok.Value);

        Assert.Equal(7, response.Views);
        Assert.Equal(0, service.UpdateCount);
    }

    private static BlogPost Published(int views) => new()
    {
        Author = "Club 12",
        Title = "A public post",
        Slug = "a-public-post",
        MarkdownText = "body",
        Views = views,
        IsPublished = true,
        CreatedBy = "test",
    };

    private static BlogPostController WithUser(BlogPostController controller, string[] roles)
    {
        List<Claim> claims = [.. roles.Select(role => new Claim(ClaimTypes.Role, role))];
        ClaimsPrincipal user = new(new ClaimsIdentity(claims, roles.Length > 0 ? "TestAuth" : null));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user },
        };
        return controller;
    }

    private sealed class RecordingBlogPostService(BlogPost post) : IBlogPostService
    {
        public int UpdateCount { get; private set; }
        public int LastPersistedViews { get; private set; }

        public Task<BlogPost?> GetBlogPostByIdOrSlugAsync(string idOrSlug, bool includeUnpublished = false)
            => Task.FromResult<BlogPost?>(post);

        public Task UpdateBlogPostAsync(BlogPost blogPostEntity)
        {
            UpdateCount++;
            LastPersistedViews = blogPostEntity.Views;
            return Task.CompletedTask;
        }

        public Task<BlogPost> CreateBlogPostAsync(BlogPost blogPostEntity)
            => throw new System.NotImplementedException();

        public Task<BlogPost?> GetBlogPostByIdAsync(System.Guid blogPostId)
            => throw new System.NotImplementedException();

        public Task DeleteBlogPostAsync(System.Guid id) => throw new System.NotImplementedException();

        public Task<PaginatedResponse<BlogPost>> GetAllBlogPostsAsync(
            GetBlogPostsFilteredRequest filter, bool includeUnpublished = false)
            => throw new System.NotImplementedException();
    }
}
