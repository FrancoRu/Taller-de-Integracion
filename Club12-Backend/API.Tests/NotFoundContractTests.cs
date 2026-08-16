using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Application.DTOs.Abstract.Response;
using Application.DTOs.Divisions.Request;
using Application.DTOs.Player.Request;
using Application.DTOs.Team.Request;
using Application.DTOs.Team.Response;
using Application.DTOs.Tournament.Request;
using Application.DTOs.Venue.Response;
using Application.Interfaces.Services;
using API.Controllers;
using AutoMapper;
using Domain.Entities.Models;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.Tests;

/// <summary>
/// Characterization/contract test proving every "resource not found" lookup across the 9
/// converted controllers returns 404 with a ProblemDetails-consistent body (matching the
/// shape GlobalExceptionHandler already emits for unhandled
/// exceptions), instead of the legacy bare-string 400. Also proves the create-time FK
/// validation (Player POST referencing a missing TeamId) deliberately stays 400 — it is
/// invalid input, not a not-found lookup.
/// </summary>
public class NotFoundContractTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public NotFoundContractTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// One GET-by-id route per controller, for the controllers whose full dependency graph
    /// can boot through CustomWebApplicationFactory anonymously (7 of the 9 —
    /// see SupabaseDependentControllerNotFoundTests for Team/Venue, which are
    /// covered via direct-controller unit tests instead; see that class' doc comment for why).
    /// </summary>
    public static readonly TheoryData<string> NotFoundGetByIdRoutes = new()
    {
        "api/matches/{0}",
        "api/blogposts/{0}",
        "api/divisions/{0}/detail",
        "api/players/{0}",
        "api/player-sanctions/{0}",
        "api/player-statistics/{0}",
        "api/tournaments/{0}",
    };

    [Theory]
    [MemberData(nameof(NotFoundGetByIdRoutes))]
    public async Task GetById_MissingEntity_Returns404ProblemDetails(string routeTemplate)
    {
        HttpClient client = _factory.CreateClient();
        Guid missingId = Guid.NewGuid();
        string url = string.Format(routeTemplate, missingId);

        HttpResponseMessage response = await client.GetAsync(url);
        string debugBody = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.NotFound, $"url={url} status={response.StatusCode} body={debugBody}");
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using JsonDocument doc = JsonDocument.Parse(debugBody);
        JsonElement root = doc.RootElement;

        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("title").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("detail").GetString()));
        Assert.Equal(404, root.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
    }

    /// <summary>
    /// Covers the spec's second scenario for this requirement ("PUT/DELETE or nested action
    /// against nonexistent parent returns 404") for the PUT-by-id case: a full HTTP-round-trip
    /// PUT against a nonexistent division id must return 404 + ProblemDetails, not 400. Prior
    /// to this test, every not-found case in this suite was GET-shaped only, even though the
    /// PUT/DELETE/nested-action sites were converted alongside the GET sites in Phase 2.
    /// </summary>
    [Fact]
    public async Task UpdateDivisionById_MissingEntity_Returns404ProblemDetails()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Owner);
        Guid missingId = Guid.NewGuid();
        UpdateDivisionRequest request = new() { Name = "Nonexistent Division", IsFinished = false };

        HttpResponseMessage response = await client.PutAsJsonAsync($"api/divisions/{missingId}", request);

        await AssertNotFoundProblemDetailsAsync(response);
    }

    /// <summary>
    /// Covers the spec's "nested action referencing a nonexistent parent id" case (the spec's
    /// own example is "adding a sanction to a nonexistent player"): registering teams to a
    /// nonexistent tournament must return 404 + ProblemDetails via a real HTTP round trip.
    /// </summary>
    [Fact]
    public async Task RegisterTeam_MissingTournament_Returns404ProblemDetails()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Owner);
        Guid missingTournamentId = Guid.NewGuid();
        RegisterTeamsInTournamentRequest request = new() { TeamIds = [Guid.NewGuid()] };

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/tournaments/register-teams/{missingTournamentId}", request);

        await AssertNotFoundProblemDetailsAsync(response);
    }

    private static async Task AssertNotFoundProblemDetailsAsync(HttpResponseMessage response)
    {
        string debugBody = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.NotFound, $"status={response.StatusCode} body={debugBody}");
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using JsonDocument doc = JsonDocument.Parse(debugBody);
        JsonElement root = doc.RootElement;

        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("title").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("detail").GetString()));
        Assert.Equal(404, root.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
    }

    /// <summary>
    /// Regression guard: the Player POST create-time FK check (missing TeamId) is
    /// invalid input, not a "resource not found" lookup, and must stay 400 — this change
    /// must NOT touch it.
    /// </summary>
    [Fact]
    public async Task CreatePlayer_MissingTeamId_StaysBadRequest()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.TeamManager);
        CreatePlayerRequest request = new()
        {
            FirstName = "Test",
            LastName = "Player",
            DocumentNumber = Guid.NewGuid().ToString("N")[..10],
            BirthDate = new DateTime(2000, 1, 1),
            PhoneNumber = "1234567890",
            SocialSecurity = "OSDE",
            TeamId = Guid.NewGuid(),
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("api/players/", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

/// <summary>
/// Covers the 2 not-found sites (TeamController.GetTeamById,
/// VenueController.GetVenueById) that cannot be exercised through
/// CustomWebApplicationFactory's full HTTP host: both controllers take a
/// constructor-injected SupabaseHelper, whose constructor eagerly opens a Supabase
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

        ActionResult<TeamResponse> result = await controller.GetTeamById(Guid.NewGuid());

        AssertNotFoundProblem(result.Result, "Team");
    }

    [Fact]
    public async Task VenueController_GetVenueById_MissingEntity_Returns404ProblemDetails()
    {
        VenueController controller = new(new NotFoundVenueService(), null!, new NotUsedMapper());
        ConfigureProblemDetailsFactory(controller);

        ActionResult<VenueResponse> result = await controller.GetVenueById(Guid.NewGuid());

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
        public Task<Team> CreateTeamAsync(Team teamEntity) => throw new NotImplementedException();
        public Task<Team?> GetTeamByIdAsync(Guid teamId) => Task.FromResult<Team?>(null);
        public Task UpdateTeamAsync(Team teamEntity) => throw new NotImplementedException();
        public Task UpdateTeamsAsync(IEnumerable<Team> teams) => throw new NotImplementedException();
        public Task DeleteTeamAsync(Guid id) => throw new NotImplementedException();
        public Task<PaginatedResponse<Team>> GetAllTeamsAsync(GetTeamsFilteredRequest filter) => throw new NotImplementedException();
        public Task RegisterTeamsToTournamentAsync(Tournament tournament, List<Guid> teamIds) => throw new NotImplementedException();
    }

    private sealed class NotFoundVenueService : IVenueService
    {
        public Task<Venue> CreateVenueAsync(Venue venueEntity) => throw new NotImplementedException();
        public Task<Venue?> GetVenueByIdAsync(Guid venueId) => Task.FromResult<Venue?>(null);
        public Task UpdateVenueAsync(Venue venueEntity) => throw new NotImplementedException();
        public Task DeleteVenueAsync(Guid id) => throw new NotImplementedException();
        public Task<IEnumerable<Venue>> GetAllVenuesAsync() => throw new NotImplementedException();
    }

    /// <summary>
    /// Never invoked — the not-found branch under test returns before any mapping happens.
    /// </summary>
    private sealed class NotUsedMapper : IMapper
    {
        public IConfigurationProvider ConfigurationProvider => throw new NotImplementedException();
        public TDestination Map<TDestination>(object source) => throw new NotImplementedException();
        public TDestination Map<TDestination>(object source, Action<IMappingOperationOptions<object, TDestination>> opts) => throw new NotImplementedException();
        public TDestination Map<TSource, TDestination>(TSource source) => throw new NotImplementedException();
        public TDestination Map<TSource, TDestination>(TSource source, Action<IMappingOperationOptions<TSource, TDestination>> opts) => throw new NotImplementedException();
        public TDestination Map<TSource, TDestination>(TSource source, TDestination destination) => throw new NotImplementedException();
        public TDestination Map<TSource, TDestination>(TSource source, TDestination destination, Action<IMappingOperationOptions<TSource, TDestination>> opts) => throw new NotImplementedException();
        public object Map(object source, Type sourceType, Type destinationType) => throw new NotImplementedException();
        public object Map(object source, Type sourceType, Type destinationType, Action<IMappingOperationOptions<object, object>> opts) => throw new NotImplementedException();
        public object Map(object source, object destination, Type sourceType, Type destinationType) => throw new NotImplementedException();
        public object Map(object source, object destination, Type sourceType, Type destinationType, Action<IMappingOperationOptions<object, object>> opts) => throw new NotImplementedException();
        public IQueryable<TDestination> ProjectTo<TDestination>(IQueryable source, object? parameters = null, params Expression<Func<TDestination, object>>[] membersToExpand) => throw new NotImplementedException();
        public IQueryable<TDestination> ProjectTo<TDestination>(IQueryable source, IDictionary<string, object> parameters, params string[] membersToExpand) => throw new NotImplementedException();
        public IQueryable ProjectTo(IQueryable source, Type destinationType, IDictionary<string, object>? parameters = null, params string[] membersToExpand) => throw new NotImplementedException();
    }
}
