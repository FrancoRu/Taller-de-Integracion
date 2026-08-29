using Domain.Entities.Models;

using Infrastructure.Persistance;

using System.Text.RegularExpressions;

namespace API.Tests;

/// <summary>
/// Regression guard for the "uuids everywhere" bug: SampleTournamentBuilder
/// used to force division/stage slug uniqueness by appending a raw
/// <see cref="System.Guid"/> to the slug source, so seeded public slugs looked
/// like "primera-division-34ce6485-e5be-4f85-80d9-e5afbb359547". Slugs must now
/// be clean kebab-case derived from the display name, with a numeric suffix
/// (-2, -3, ...) as the only disambiguator, and must stay unique across the
/// whole seeded batch so the DB unique indexes are never violated.
/// </summary>
public class SampleTournamentBuilderSlugTests
{
    // 8-4-4-4-12 lowercase-hex groups: exactly how a raw GUID survives slugging.
    private static readonly Regex GuidPattern =
        new("[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}", RegexOptions.Compiled);

    private static readonly Regex KebabPattern =
        new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled);

    [Fact]
    public void Build_DivisionAndStageSlugs_AreCleanKebabWithoutGuid()
    {
        List<Venue> venues =
        [
            new() { Slug = "venue-uno", CreatedBy = "test", Name = "Cancha Uno", Address = "Calle 1" },
            new() { Slug = "venue-dos", CreatedBy = "test", Name = "Cancha Dos", Address = "Calle 2" },
        ];

        SampleTournamentBuilder.TournamentDefinition definition = new(
            Name: "Apertura Prueba",
            Description: "Torneo de prueba con playoffs y copa cruzada.",
            TeamRegistrationDeadline: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            StartDate: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            StageStartDate: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            StageEndDate: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            FinishedMatchesStart: new DateTime(2026, 2, 8, 0, 0, 0, DateTimeKind.Utc),
            UpcomingMatchesStart: new DateTime(2026, 3, 8, 0, 0, 0, DateTimeKind.Utc),
            Divisions:
            [
                new(
                    "Primera División",
                    ["Equipo A", "Equipo B", "Equipo C", "Equipo D"],
                    ["EQA", "EQB", "EQC", "EQD"],
                    ["#111111", "#222222", "#333333", "#444444"]),
                new(
                    "Segunda División",
                    ["Equipo E", "Equipo F", "Equipo G", "Equipo H"],
                    ["EQE", "EQF", "EQG", "EQH"],
                    ["#555555", "#666666", "#777777", "#888888"]),
            ],
            CrossCup: new("Copa Club 12", GroupCount: 2, QualifiersPerGroup: 1));

        int playerCounter = 0;
        SampleTournamentBuilder.BuildResult result =
            SampleTournamentBuilder.Build(definition, venues, ref playerCounter, includePlayoffs: true);

        List<Division> divisions = [.. result.Tournament.Divisions];
        List<Stage> stages = [.. divisions.SelectMany(d => d.Stages)];

        Assert.NotEmpty(divisions);
        Assert.NotEmpty(stages);

        foreach (Division division in divisions)
        {
            AssertCleanSlug(division.Slug);
        }

        foreach (Stage stage in stages)
        {
            AssertCleanSlug(stage.Slug);
        }

        // The clean cross-cup division slug is exactly the kebab of its name.
        Assert.Contains(divisions, d => d.Slug == "primera-division");
        Assert.Contains(divisions, d => d.Slug == "copa-club-12");

        // Slugs stay unique within each table so the DB unique indexes hold.
        List<string> divisionSlugs = [.. divisions.Select(d => d.Slug)];
        List<string> stageSlugs = [.. stages.Select(s => s.Slug)];
        Assert.Equal(divisionSlugs.Count, divisionSlugs.Distinct().Count());
        Assert.Equal(stageSlugs.Count, stageSlugs.Distinct().Count());
    }

    [Fact]
    public void Build_SharedRegistryAcrossTournaments_KeepsStageSlugsUniqueWithNumericSuffix()
    {
        List<Venue> venues =
        [
            new() { Slug = "venue-uno", CreatedBy = "test", Name = "Cancha Uno", Address = "Calle 1" },
        ];

        SampleTournamentBuilder.TournamentDefinition Make(string name) => new(
            Name: name,
            Description: "Torneo de prueba.",
            TeamRegistrationDeadline: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            StartDate: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            StageStartDate: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            StageEndDate: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            FinishedMatchesStart: new DateTime(2026, 2, 8, 0, 0, 0, DateTimeKind.Utc),
            UpcomingMatchesStart: new DateTime(2026, 3, 8, 0, 0, 0, DateTimeKind.Utc),
            Divisions:
            [
                new(
                    "Primera",
                    ["Equipo A", "Equipo B", "Equipo C", "Equipo D"],
                    ["EQA", "EQB", "EQC", "EQD"],
                    ["#111111", "#222222", "#333333", "#444444"]),
            ]);

        int playerCounter = 0;
        SampleTournamentBuilder.SlugRegistry slugRegistry = new();
        SampleTournamentBuilder.BuildResult first =
            SampleTournamentBuilder.Build(Make("Torneo Uno"), venues, ref playerCounter, includePlayoffs: false, slugRegistry);
        SampleTournamentBuilder.BuildResult second =
            SampleTournamentBuilder.Build(Make("Torneo Dos"), venues, ref playerCounter, includePlayoffs: false, slugRegistry);

        List<string> allStageSlugs =
        [
            .. first.Tournament.Divisions.SelectMany(d => d.Stages).Select(s => s.Slug),
            .. second.Tournament.Divisions.SelectMany(d => d.Stages).Select(s => s.Slug),
        ];

        foreach (string slug in allStageSlugs)
        {
            AssertCleanSlug(slug);
        }

        // Both tournaments have a "Fase de Grupos" group stage for the same
        // division name, so the shared registry disambiguates the second with a
        // numeric suffix instead of a GUID.
        Assert.Contains("fase-de-grupos-primera", allStageSlugs);
        Assert.Contains("fase-de-grupos-primera-2", allStageSlugs);
        Assert.Equal(allStageSlugs.Count, allStageSlugs.Distinct().Count());
    }

    private static void AssertCleanSlug(string slug)
    {
        Assert.False(string.IsNullOrWhiteSpace(slug));
        Assert.DoesNotMatch(GuidPattern, slug);
        Assert.Matches(KebabPattern, slug);
    }
}
