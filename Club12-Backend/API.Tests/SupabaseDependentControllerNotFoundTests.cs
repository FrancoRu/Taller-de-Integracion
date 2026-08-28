using API.Controllers;

using Application.DTOs.Abstract.Response;
using Application.DTOs.BlogPosts.Request;
using Application.DTOs.BlogPosts.Response;
using Application.DTOs.Team.Request;
using Application.DTOs.Team.Response;
using Application.DTOs.Venue.Response;
using Application.Interfaces.Services;

using AutoMapper;

using Domain.Entities.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using System.Linq.Expressions;

namespace API.Tests;

/// <summary>
/// Covers the 3 not-found sites (TeamController.GetTeamById,
/// VenueController.GetVenueById, BlogPostController.GetBlogPostById) that cannot be
/// exercised through CustomWebApplicationFactory's full HTTP host: all three controllers
/// take a constructor-injected SupabaseHelper, whose constructor eagerly opens a Supabase
/// Realtime websocket connection (AutoConnectRealtime = true) — this throws/hangs in
/// any sandboxed environment without live Supabase network access. That is a pre-existing
/// production-code testability gap, unrelated to and out of scope for this 400→404 fix, so
/// this test degrades to the next available layer per the TDD "Choosing Test Layer" rule:
/// direct-controller unit tests with hand-rolled service fakes, bypassing DI entirely.
/// SupabaseHelper is passed as null! because the not-found branch under test
/// never touches it.
/// </summary>
public class SupabaseDependentControllerNotFoundTests
{
    [Fact]
    public async Task TeamController_GetTeamById_MissingEntity_Returns404ProblemDetails()
    {
        TeamController controller = new(new NotFoundTeamService(), null!, new NotUsedMapper());
        ConfigureProblemDetailsFactory(controller);

        ActionResult<TeamResponse> result = await controller.GetTeamById(Guid.NewGuid().ToString());

        AssertNotFoundProblem(result.Result, "Team");
    }

    [Fact]
    public async Task VenueController_GetVenueById_MissingEntity_Returns404ProblemDetails()
    {
        VenueController controller = new(new NotFoundVenueService(), null!, new NotUsedMapper());
        ConfigureProblemDetailsFactory(controller);

        ActionResult<VenueResponse> result = await controller.GetVenueById(Guid.NewGuid().ToString());

        AssertNotFoundProblem(result.Result, "Venue");
    }

    /// <summary>
    /// Covers the spec's DELETE case ("PUT/DELETE or nested action against nonexistent parent
    /// returns 404") for the sites that hit the same Supabase testability gap as the GET cases
    /// above: TeamController.DeleteTeamById checks for the entity via
    /// ITeamService.GetTeamByIdAsync and returns 404 before ever touching
    /// SupabaseHelper, so the same null!-SupabaseHelper direct-controller pattern
    /// applies here.
    /// </summary>
    [Fact]
    public async Task TeamController_DeleteTeamById_MissingEntity_Returns404ProblemDetails()
    {
        TeamController controller = new(new NotFoundTeamService(), null!, new NotUsedMapper());
        ConfigureProblemDetailsFactory(controller);

        IActionResult result = await controller.DeleteTeamById(Guid.NewGuid());

        AssertNotFoundProblem(result as ActionResult, "Team");
    }

    [Fact]
    public async Task BlogPostController_GetBlogPostById_MissingEntity_Returns404ProblemDetails()
    {
        BlogPostController controller = new(new NotFoundBlogPostService(), null!, new NotUsedMapper());
        ConfigureProblemDetailsFactory(controller);

        ActionResult<BlogPostResponse> result = await controller.GetBlogPostById(Guid.NewGuid().ToString());

        AssertNotFoundProblem(result.Result, "BlogPost");
    }

    private static void AssertNotFoundProblem(ActionResult? actionResult, string expectedEntity)
    {
        ObjectResult objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);

        ProblemDetails problemDetails = Assert.IsAssignableFrom<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problemDetails.Status);
        Assert.False(string.IsNullOrWhiteSpace(problemDetails.Title));
        Assert.Contains(expectedEntity, problemDetails.Detail);
    }

    /// <summary>
    /// Gives the controller just enough of an HttpContext for
    /// ControllerBase.Problem() to resolve a real ProblemDetailsFactory
    /// (the same one the full host uses), without booting the whole application.
    /// </summary>
    private static void ConfigureProblemDetailsFactory(ControllerBase controller)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddMvc();
        ServiceProvider provider = services.BuildServiceProvider();

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = provider },
        };
    }

    private sealed class NotFoundTeamService : ITeamService
    {
        public Task<Team> CreateTeamAsync(Team teamEntity)
        {
            throw new NotImplementedException();
        }

        public Task<Team?> GetTeamByIdAsync(Guid teamId, Guid? tournamentId = null)
        {
            return Task.FromResult<Team?>(null);
        }

        public Task<Team?> GetTeamByIdOrSlugAsync(string idOrSlug, Guid? tournamentId = null)
        {
            return Task.FromResult<Team?>(null);
        }

        public Task UpdateTeamAsync(Team teamEntity)
        {
            throw new NotImplementedException();
        }

        public Task UpdateTeamsAsync(IEnumerable<Team> teams)
        {
            throw new NotImplementedException();
        }

        public Task DeleteTeamAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<PaginatedResponse<Team>> GetAllTeamsAsync(GetTeamsFilteredRequest filter)
        {
            throw new NotImplementedException();
        }

        public Task RegisterTeamsToTournamentAsync(Tournament tournament, List<Guid> teamIds)
        {
            throw new NotImplementedException();
        }

        public Task<Team> EnrollTeamAsync(
            Tournament tournament,
            Guid? existingTeamId,
            string? newTeamName,
            Guid? copyRosterFromTournamentId)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class NotFoundVenueService : IVenueService
    {
        public Task<Venue> CreateVenueAsync(Venue venueEntity)
        {
            throw new NotImplementedException();
        }

        public Task<Venue?> GetVenueByIdAsync(Guid venueId)
        {
            return Task.FromResult<Venue?>(null);
        }

        public Task<Venue?> GetVenueByIdOrSlugAsync(string idOrSlug)
        {
            return Task.FromResult<Venue?>(null);
        }

        public Task UpdateVenueAsync(Venue venueEntity)
        {
            throw new NotImplementedException();
        }

        public Task DeleteVenueAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Venue>> GetAllVenuesAsync()
        {
            throw new NotImplementedException();
        }
    }

    private sealed class NotFoundBlogPostService : IBlogPostService
    {
        public Task<BlogPost> CreateBlogPostAsync(BlogPost blogPostEntity)
        {
            throw new NotImplementedException();
        }

        public Task<BlogPost?> GetBlogPostByIdAsync(Guid blogPostId)
        {
            return Task.FromResult<BlogPost?>(null);
        }

        public Task<BlogPost?> GetBlogPostByIdOrSlugAsync(string idOrSlug, bool includeUnpublished = false)
        {
            return Task.FromResult<BlogPost?>(null);
        }

        public Task DeleteBlogPostAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task UpdateBlogPostAsync(BlogPost blogPostEntity)
        {
            throw new NotImplementedException();
        }

        public Task<PaginatedResponse<BlogPost>> GetAllBlogPostsAsync(GetBlogPostsFilteredRequest filter, bool includeUnpublished = false)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Never invoked — the not-found branch under test returns before any mapping happens.
    /// </summary>
    private sealed class NotUsedMapper : IMapper
    {
        public IConfigurationProvider ConfigurationProvider => throw new NotImplementedException();
        public TDestination Map<TDestination>(object source)
        {
            throw new NotImplementedException();
        }

        public TDestination Map<TDestination>(object source, Action<IMappingOperationOptions<object, TDestination>> opts)
        {
            throw new NotImplementedException();
        }

        public TDestination Map<TSource, TDestination>(TSource source)
        {
            throw new NotImplementedException();
        }

        public TDestination Map<TSource, TDestination>(TSource source, Action<IMappingOperationOptions<TSource, TDestination>> opts)
        {
            throw new NotImplementedException();
        }

        public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
        {
            throw new NotImplementedException();
        }

        public TDestination Map<TSource, TDestination>(TSource source, TDestination destination, Action<IMappingOperationOptions<TSource, TDestination>> opts)
        {
            throw new NotImplementedException();
        }

        public object Map(object source, Type sourceType, Type destinationType)
        {
            throw new NotImplementedException();
        }

        public object Map(object source, Type sourceType, Type destinationType, Action<IMappingOperationOptions<object, object>> opts)
        {
            throw new NotImplementedException();
        }

        public object Map(object source, object destination, Type sourceType, Type destinationType)
        {
            throw new NotImplementedException();
        }

        public object Map(object source, object destination, Type sourceType, Type destinationType, Action<IMappingOperationOptions<object, object>> opts)
        {
            throw new NotImplementedException();
        }

        public IQueryable<TDestination> ProjectTo<TDestination>(IQueryable source, object? parameters = null, params Expression<Func<TDestination, object>>[] membersToExpand)
        {
            throw new NotImplementedException();
        }

        public IQueryable<TDestination> ProjectTo<TDestination>(IQueryable source, IDictionary<string, object> parameters, params string[] membersToExpand)
        {
            throw new NotImplementedException();
        }

        public IQueryable ProjectTo(IQueryable source, Type destinationType, IDictionary<string, object>? parameters = null, params string[] membersToExpand)
        {
            throw new NotImplementedException();
        }
    }
}
