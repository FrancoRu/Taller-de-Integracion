using Application.Interfaces.Services;

using Domain.Entities.Models;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// Verifies BlogPostService's slug support: CreateBlogPostAsync generates a
/// unique slug from the post's Title, and GetBlogPostByIdOrSlugAsync resolves
/// a post by either its GUID id or its slug. Exercised at the service layer
/// (resolved directly from DI) rather than through BlogPostController,
/// because the controller's constructor-injected SupabaseHelper eagerly
/// opens a Supabase Realtime websocket connection and cannot boot in a
/// sandboxed test environment — see SupabaseDependentControllerNotFoundTests'
/// doc comment for the same testability gap on this controller's not-found
/// case.
/// </summary>
public class BlogPostServiceSlugTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public BlogPostServiceSlugTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateBlogPostAsync_GeneratesSlugFromTitle()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IBlogPostService blogPostService = scope.ServiceProvider.GetRequiredService<IBlogPostService>();

        BlogPost created = await blogPostService.CreateBlogPostAsync(new BlogPost
        {
            Author = "Autor de Prueba",
            Title = $"Título con Ñandú {Guid.NewGuid():N}",
            MarkdownText = "contenido",
            CreatedBy = "test",
            Slug = null!,
        });

        Assert.False(string.IsNullOrWhiteSpace(created.Slug));
        Assert.DoesNotContain(' ', created.Slug);
        Assert.Equal(created.Slug, created.Slug.ToLowerInvariant());
    }

    [Fact]
    public async Task CreateBlogPostAsync_DuplicateTitle_AppendsSuffixToSlug()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IBlogPostService blogPostService = scope.ServiceProvider.GetRequiredService<IBlogPostService>();

        string sharedTitle = $"Mismo Titulo {Guid.NewGuid():N}";

        BlogPost first = await blogPostService.CreateBlogPostAsync(new BlogPost
        {
            Author = "Autor Uno",
            Title = sharedTitle,
            MarkdownText = "contenido uno",
            CreatedBy = "test",
            Slug = null!,
        });

        BlogPost second = await blogPostService.CreateBlogPostAsync(new BlogPost
        {
            Author = "Autor Dos",
            Title = sharedTitle,
            MarkdownText = "contenido dos",
            CreatedBy = "test",
            Slug = null!,
        });

        Assert.NotEqual(first.Slug, second.Slug);
        Assert.Equal($"{first.Slug}-2", second.Slug);
    }

    [Fact]
    public async Task GetBlogPostByIdOrSlugAsync_ResolvesByGuidId()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IBlogPostService blogPostService = scope.ServiceProvider.GetRequiredService<IBlogPostService>();

        BlogPost created = await blogPostService.CreateBlogPostAsync(new BlogPost
        {
            Author = "Autor",
            Title = $"Post por id {Guid.NewGuid():N}",
            MarkdownText = "contenido",
            CreatedBy = "test",
            Slug = null!,
        });

        BlogPost? found = await blogPostService.GetBlogPostByIdOrSlugAsync(created.Id.ToString());

        Assert.NotNull(found);
        Assert.Equal(created.Id, found!.Id);
    }

    [Fact]
    public async Task GetBlogPostByIdOrSlugAsync_ResolvesBySlug()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IBlogPostService blogPostService = scope.ServiceProvider.GetRequiredService<IBlogPostService>();

        BlogPost created = await blogPostService.CreateBlogPostAsync(new BlogPost
        {
            Author = "Autor",
            Title = $"Post por slug {Guid.NewGuid():N}",
            MarkdownText = "contenido",
            CreatedBy = "test",
            Slug = null!,
        });

        BlogPost? found = await blogPostService.GetBlogPostByIdOrSlugAsync(created.Slug);

        Assert.NotNull(found);
        Assert.Equal(created.Id, found!.Id);
    }

    [Fact]
    public async Task GetBlogPostByIdOrSlugAsync_UnknownSlug_ReturnsNull()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IBlogPostService blogPostService = scope.ServiceProvider.GetRequiredService<IBlogPostService>();

        BlogPost? found = await blogPostService.GetBlogPostByIdOrSlugAsync($"unknown-slug-{Guid.NewGuid():N}");

        Assert.Null(found);
    }
}
