using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Interfaces.Services;
using Application.Utils.Constants.Stage;
using Domain.Entities.Models;
using Domain.Enums;
using Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Characterization (approval) test for
/// <see cref="Application.Services.MatchService.CreateAutomatedMatchesAsync"/>'s knockout and
/// final-stage match generation. Written BEFORE the batch1 cleanup refactor (magic-number
/// extraction, CS1998 fix) to prove those edits stay behavior-preserving: match counts, stage
/// assignment (via <see cref="MatchType.Playoff"/>), and date bounds must remain identical.
/// </summary>
public class AutomatedMatchGenerationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AutomatedMatchGenerationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public static readonly TheoryData<StageType, int> KnockoutStageCases = new()
    {
        { StageType.QuarterFinal, KnockoutMatchCount.QUARTER_FINAL },
        { StageType.SemiFinal, KnockoutMatchCount.SEMI_FINAL },
        { StageType.Final, 1 },
        { StageType.ThirdPlace, 1 },
    };

    [Theory]
    [MemberData(nameof(KnockoutStageCases))]
    public async Task CreateAutomatedMatchesAsync_GeneratesExpectedMatchCountForKnockoutStage(
        StageType stageType, int expectedMatchCount)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        Stage stage = await SeedStageAsync(db, stageType);

        List<Match> matches = await matchService.CreateAutomatedMatchesAsync(stage.Id);

        Assert.Equal(expectedMatchCount, matches.Count);
        Assert.All(matches, match => Assert.Equal(MatchType.Playoff, match.Type));
        Assert.All(matches, match => Assert.InRange(match.MatchDate, stage.StartDate, stage.EndDate));
    }

    [Fact]
    public void KnockoutMatchCount_MatchesPriorLiterals()
    {
        Assert.Equal(4, KnockoutMatchCount.QUARTER_FINAL);
        Assert.Equal(2, KnockoutMatchCount.SEMI_FINAL);
    }

    private static async Task<Stage> SeedStageAsync(ApplicationDBContext db, StageType stageType)
    {
        DateTime startDate = DateTime.UtcNow.Date;
        DateTime endDate = startDate.AddDays(14);

        Guid divisionId = Guid.NewGuid();

        Tournament tournament = new()
        {
            Description = "Characterization test tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = startDate.AddDays(-1),
            StartDate = startDate,
            MaxTeams = 8,
            MinTeams = 2,
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };

        Division division = new()
        {
            Id = divisionId,
            Name = $"Division-{Guid.NewGuid()}",
            Tournament = tournament,
            Stages = [],
            CreatedBy = "test",
        };

        Stage stage = new()
        {
            Name = $"Stage-{Guid.NewGuid()}",
            StageType = stageType,
            IsActive = true,
            StartDate = startDate,
            EndDate = endDate,
            DivisionId = divisionId,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };

        db.Tournaments.Add(tournament);
        db.Divisions.Add(division);
        db.Stages.Add(stage);
        await db.SaveChangesAsync();

        return stage;
    }
}
