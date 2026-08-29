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
}
