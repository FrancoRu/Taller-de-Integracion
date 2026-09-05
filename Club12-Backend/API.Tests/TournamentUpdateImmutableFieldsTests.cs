using Application.DTOs.Tournament.Request;

using AutoMapper;

using Domain.Entities.Models;
using Domain.Enums;

using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// A tournament's <see cref="Tournament.StartDate"/> drives when it is
/// understood to have happened (season grouping, champions history, calendar)
/// and must never move after creation, regardless of status — the frontend
/// edit form never exposes it, but that alone is not a real guard against a
/// direct API call. Verifies <c>CreateMap&lt;UpdateTournamentRequest, Tournament&gt;()</c>
/// ignores <see cref="Tournament.StartDate"/> the same way it already ignores
/// <see cref="Tournament.Status"/> and <see cref="Tournament.Category"/>.
/// </summary>
public class TournamentUpdateImmutableFieldsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TournamentUpdateImmutableFieldsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void MappingUpdateRequestOntoTournament_DifferentStartDate_LeavesOriginalStartDateUnchanged()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IMapper mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        DateTime originalStartDate = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        Tournament tournament = new()
        {
            Name = "Original",
            Description = "Original",
            Slug = "original",
            TeamRegistrationDeadline = originalStartDate.AddDays(-1),
            StartDate = originalStartDate,
            Status = TournamentStatus.OpenForRegistration,
            Category = TournamentCategory.Masculine,
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };

        UpdateTournamentRequest request = new()
        {
            Name = "Renamed",
            Description = "Renamed",
            TeamRegistrationDeadline = originalStartDate.AddDays(-1),
            StartDate = originalStartDate.AddYears(1),
            Status = TournamentStatus.Canceled,
        };

        mapper.Map(request, tournament);

        Assert.Equal(originalStartDate, tournament.StartDate);
        Assert.Equal("Renamed", tournament.Name);
    }
}
