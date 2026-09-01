using Application.DTOs.Match.Request;

using AutoMapper;

using Domain.Entities.Models;
using Domain.Enums;

using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// Rescheduling a match (edit date/venue) must NEVER clear its teams. A prior
/// bug had HomeTeamId/VisitorTeamId on UpdateMatchRequest, so the convention
/// map wrote their (unsent) null values over the entity and wiped both teams.
/// </summary>
public class MatchRescheduleMappingTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public MatchRescheduleMappingTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void MappingRescheduleRequest_KeepsTeamsAndUpdatesDateAndVenue()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IMapper mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        Guid homeTeamId = Guid.NewGuid();
        Guid visitorTeamId = Guid.NewGuid();
        Guid newVenueId = Guid.NewGuid();
        DateTime newDate = DateTime.UtcNow.Date.AddDays(7);

        Match match = new()
        {
            Slug = "team-a-vs-team-b-2026-10-04",
            Type = Domain.Enums.MatchType.Regular,
            IsFinished = false,
            CreatedBy = Domain.Constants.AuditConstants.SystemUser,
            HomeTeamId = homeTeamId,
            VisitorTeamId = visitorTeamId,
            MatchDate = DateTime.UtcNow.Date,
        };

        // Exactly what MatchController.UpdateMatchDate binds and maps: only the
        // calendar date and the venue — no team ids.
        UpdateMatchRequest request = new() { MatchDate = newDate, VenueId = newVenueId };
        mapper.Map(request, match);

        // Teams survive the reschedule.
        Assert.Equal(homeTeamId, match.HomeTeamId);
        Assert.Equal(visitorTeamId, match.VisitorTeamId);
        // Date and venue were applied.
        Assert.Equal(newDate, match.MatchDate);
        Assert.Equal(newVenueId, match.VenueId);
    }
}
