using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

namespace API.Tests;

/// <summary>
/// Covers the seed builder's category support (HU-48): a
/// <see cref="SampleTournamentBuilder.TournamentDefinition.Category"/> must flow
/// onto the built <see cref="Tournament.Category"/> and onto EVERY division —
/// the regular divisions AND the cross-division cup — so the seeded graph never
/// mixes feminine and masculine divisions. Also guards that the cross-division
/// cup division is actually flagged and that seeded teams always carry a
/// (placeholder) logo before any real upload runs.
/// </summary>
public class SampleTournamentBuilderCategoryTests
{
    private static List<Venue> BuildVenues() =>
    [
        new() { Slug = "venue-uno", CreatedBy = "test", Name = "Cancha Uno", Address = "Calle 1" },
        new() { Slug = "venue-dos", CreatedBy = "test", Name = "Cancha Dos", Address = "Calle 2" },
    ];

    private static SampleTournamentBuilder.TournamentDefinition MakeDefinition(
        string name, TournamentCategory category) => new(
        Name: name,
        Description: "Torneo de prueba con copa cruzada.",
        TeamRegistrationDeadline: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        StartDate: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
        StageStartDate: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
        StageEndDate: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
        FinishedMatchesStart: new DateTime(2026, 2, 8, 0, 0, 0, DateTimeKind.Utc),
        UpcomingMatchesStart: new DateTime(2026, 3, 8, 0, 0, 0, DateTimeKind.Utc),
        Divisions:
        [
            new(
                "Damas A",
                ["Equipo A", "Equipo B", "Equipo C", "Equipo D"],
                ["EQA", "EQB", "EQC", "EQD"],
                ["#111111", "#222222", "#333333", "#444444"]),
        ],
        CrossCup: new("Copa Club 12", GroupCount: 2, QualifiersPerGroup: 1),
        Category: category);

    [Fact]
    public void Build_DefaultCategory_IsMasculineOnTournamentAndDivisions()
    {
        SampleTournamentBuilder.TournamentDefinition definition = MakeDefinition(
            "Apertura Masculino", TournamentCategory.Masculine);

        int playerCounter = 0;
        SampleTournamentBuilder.BuildResult result =
            SampleTournamentBuilder.Build(definition, BuildVenues(), ref playerCounter, includePlayoffs: true);

        Assert.Equal(TournamentCategory.Masculine, result.Tournament.Category);
        Assert.All(result.Tournament.Divisions,
            d => Assert.Equal(TournamentCategory.Masculine, d.Category));
    }

    [Fact]
    public void Build_FeminineCategory_FlowsToTournamentAndEveryDivisionIncludingCup()
    {
        SampleTournamentBuilder.TournamentDefinition definition = MakeDefinition(
            "Apertura Femenino", TournamentCategory.Feminine);

        int playerCounter = 0;
        SampleTournamentBuilder.BuildResult result =
            SampleTournamentBuilder.Build(definition, BuildVenues(), ref playerCounter, includePlayoffs: true);

        Assert.Equal(TournamentCategory.Feminine, result.Tournament.Category);

        // Every division — regular zones AND the cross-division cup — shares the
        // tournament's category.
        Assert.All(result.Tournament.Divisions,
            d => Assert.Equal(TournamentCategory.Feminine, d.Category));
    }

    [Fact]
    public void Build_WithCrossCup_ProducesFlaggedCupDivision()
    {
        SampleTournamentBuilder.TournamentDefinition definition = MakeDefinition(
            "Apertura Femenino", TournamentCategory.Feminine);

        int playerCounter = 0;
        SampleTournamentBuilder.BuildResult result =
            SampleTournamentBuilder.Build(definition, BuildVenues(), ref playerCounter, includePlayoffs: true);

        Division cup = Assert.Single(result.Tournament.Divisions, d => d.IsCrossDivisionCup);
        Assert.Equal("Copa Club 12", cup.Name);
        Assert.Equal(TournamentCategory.Feminine, cup.Category);
    }

    [Fact]
    public void Build_EverySeededTeam_HasANonEmptyLogoUrl()
    {
        SampleTournamentBuilder.TournamentDefinition definition = MakeDefinition(
            "Apertura Masculino", TournamentCategory.Masculine);

        int playerCounter = 0;
        SampleTournamentBuilder.BuildResult result =
            SampleTournamentBuilder.Build(definition, BuildVenues(), ref playerCounter, includePlayoffs: true);

        Assert.NotEmpty(result.Tournament.Teams);
        Assert.All(result.Tournament.Teams, t => Assert.False(string.IsNullOrWhiteSpace(t.LogoUrl)));
    }

    /// <summary>
    /// An in-progress (ONGOING) tournament seeded as a double round-robin
    /// (RoundRobinLegs = 2) with a played-rounds cutoff finishes exactly the
    /// leading jornadas and leaves the rest as upcoming (unplayed) games — the
    /// shape the Clausura uses so its standings AND "Próximos" fixture both have
    /// data. Finished games carry a decisive score and winner; upcoming games
    /// carry none.
    /// </summary>
    [Fact]
    public void Build_OngoingDoubleRoundRobin_FinishesLeadingJornadasAndLeavesRestUpcoming()
    {
        SampleTournamentBuilder.TournamentDefinition definition = new(
            Name: "Clausura En Curso",
            Description: "Torneo en curso a dos ruedas.",
            TeamRegistrationDeadline: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            StartDate: new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc),
            StageStartDate: new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc),
            StageEndDate: new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            FinishedMatchesStart: new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc),
            UpcomingMatchesStart: new DateTime(2026, 9, 6, 0, 0, 0, DateTimeKind.Utc),
            Divisions:
            [
                new(
                    "Primera División",
                    ["Equipo A", "Equipo B", "Equipo C", "Equipo D"],
                    ["EQA", "EQB", "EQC", "EQD"],
                    ["#111111", "#222222", "#333333", "#444444"]),
            ],
            Status: TournamentStatus.Ongoing,
            RoundRobinLegs: 2,
            PlayedRoundsPerZone: 3);

        int playerCounter = 0;
        SampleTournamentBuilder.BuildResult result =
            SampleTournamentBuilder.Build(definition, BuildVenues(), ref playerCounter, includePlayoffs: false);

        // Group-only: no playoffs while the tournament is in progress.
        Stage groupStage = Assert.Single(result.Tournament.Divisions.Single().Stages);
        Assert.Equal(StageType.Group, groupStage.StageType);
        Assert.Equal(2, groupStage.RoundRobinLegs);

        // 4 teams double round-robin = 6 jornadas (1..6); the first 3 are played.
        List<int> finishedRounds = [.. groupStage.Matches.Where(m => m.IsFinished).Select(m => m.Round!.Value).Distinct().OrderBy(r => r)];
        List<int> upcomingRounds = [.. groupStage.Matches.Where(m => !m.IsFinished).Select(m => m.Round!.Value).Distinct().OrderBy(r => r)];

        Assert.Equal([1, 2, 3], finishedRounds);
        Assert.Equal([4, 5, 6], upcomingRounds);

        // Finished games have a decisive result and a winner; upcoming games do not.
        Assert.All(groupStage.Matches.Where(m => m.IsFinished), m =>
        {
            Assert.NotNull(m.HomeScore);
            Assert.NotNull(m.VisitorScore);
            Assert.NotNull(m.WinningTeamId);
        });
        Assert.All(groupStage.Matches.Where(m => !m.IsFinished), m =>
        {
            Assert.Null(m.HomeScore);
            Assert.Null(m.VisitorScore);
            Assert.Null(m.WinningTeamId);
            Assert.Equal(MatchStatus.Scheduled, m.Status);
        });

        // Upcoming jornadas are scheduled on later dates than the played ones.
        DateTime lastPlayed = groupStage.Matches.Where(m => m.IsFinished).Max(m => m.MatchDate);
        DateTime firstUpcoming = groupStage.Matches.Where(m => !m.IsFinished).Min(m => m.MatchDate);
        Assert.True(firstUpcoming > lastPlayed);
    }
}
