using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

namespace API.Tests;

/// <summary>
/// Covers SampleTournamentBuilder.SeedEliminationBracket: a playoff cup whose
/// position range has more than 4 real seeds (e.g. a 9-team Copa Plata, byes
/// padding the bracket to 16) must build a FULL multi-round bracket
/// (RoundOf16 -> QuarterFinal -> SemiFinal -> Final) where every round's
/// winners actually advance and play the next round — not just the fixed
/// SemiFinal + Final shape older/smaller (exactly 4-seed) cups use, which
/// used to silently drop every seed past the first two round-1 winners.
/// BestOf only applies to the SemiFinal and Final rounds; earlier rounds
/// always play Bo1.
/// </summary>
public class SampleTournamentBuilderEliminationBracketTests
{
    private static List<Venue> BuildVenues() =>
    [
        new() { Slug = "venue-uno", CreatedBy = "test", Name = "Cancha Uno", Address = "Calle 1" },
        new() { Slug = "venue-dos", CreatedBy = "test", Name = "Cancha Dos", Address = "Calle 2" },
    ];

    private static SampleTournamentBuilder.TournamentDefinition MakeDefinition(
        int teamCount, SampleTournamentBuilder.PlayoffCupDefinition[] cups)
    {
        string[] names = [.. Enumerable.Range(1, teamCount).Select(i => $"Equipo {i}")];
        string[] codes = [.. Enumerable.Range(1, teamCount).Select(i => $"EQ{i:00}")];
        string[] colors = [.. Enumerable.Range(1, teamCount).Select(_ => "#111111")];

        return new(
            Name: "Torneo de prueba",
            Description: "Torneo de prueba con un cupo de playoffs mayor a 4 equipos.",
            TeamRegistrationDeadline: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            StartDate: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            StageStartDate: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            StageEndDate: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            FinishedMatchesStart: new DateTime(2026, 2, 8, 0, 0, 0, DateTimeKind.Utc),
            UpcomingMatchesStart: new DateTime(2026, 3, 8, 0, 0, 0, DateTimeKind.Utc),
            Divisions:
            [
                new("Zona Única", names, codes, colors, cups),
            ]);
    }

    [Fact]
    public void Build_CupWithNineSeeds_BuildsFullRoundOf16ToFinalBracket()
    {
        SampleTournamentBuilder.PlayoffCupDefinition[] cups =
            [new("Copa Plata", FromPosition: 1, ToPosition: 9, BestOf: 3)];
        SampleTournamentBuilder.TournamentDefinition definition = MakeDefinition(9, cups);

        int playerCounter = 0;
        SampleTournamentBuilder.BuildResult result =
            SampleTournamentBuilder.Build(definition, BuildVenues(), ref playerCounter, includePlayoffs: true);

        Division division = result.Tournament.Divisions.Single();
        List<Stage> cupStages = [.. division.Stages.Where(s => s.BracketName == "Copa Plata")];

        // 9 seeds -> next power of two (16): RoundOf16, QuarterFinal, SemiFinal, Final.
        Assert.Equal(4, cupStages.Count);
        Assert.Contains(cupStages, s => s.StageType == StageType.RoundOf16);
        Assert.Contains(cupStages, s => s.StageType == StageType.QuarterFinal);
        Assert.Contains(cupStages, s => s.StageType == StageType.SemiFinal);
        Assert.Contains(cupStages, s => s.StageType == StageType.Final);

        Stage roundOf16 = cupStages.Single(s => s.StageType == StageType.RoundOf16);
        Stage quarterFinal = cupStages.Single(s => s.StageType == StageType.QuarterFinal);
        Stage semiFinal = cupStages.Single(s => s.StageType == StageType.SemiFinal);
        Stage final = cupStages.Single(s => s.StageType == StageType.Final);

        // 9 real seeds padded to 16 = 7 byes + 1 real match in round 1.
        Assert.Equal(9, roundOf16.StageTeamMatches.Count);
        Assert.Single(roundOf16.Matches);

        // Every later round is bye-free: the 8 round-1 winners (7 byes plus
        // the one real match's winner) actually play on, halving each round
        // down to the champion.
        Assert.Equal(8, quarterFinal.StageTeamMatches.Count);
        Assert.Equal(4, quarterFinal.Matches.Count);
        Assert.Equal(4, semiFinal.StageTeamMatches.Count);
        Assert.Equal(2, semiFinal.Matches.Count);
        Assert.Equal(2, final.StageTeamMatches.Count);
        Match finalMatch = Assert.Single(final.Matches);
        Assert.True(finalMatch.IsFinished);
        Assert.NotNull(finalMatch.WinningTeamId);

        // Series length applies only to the last two rounds.
        // Earlier rounds are always single games.
        Assert.Equal(1, roundOf16.BestOf);
        Assert.Equal(1, quarterFinal.BestOf);
        Assert.Equal(3, semiFinal.BestOf);
        Assert.Equal(3, final.BestOf);
    }

    [Fact]
    public void Build_CupWithFourSeeds_KeepsTheClassicSemiFinalPlusFinalShape()
    {
        SampleTournamentBuilder.PlayoffCupDefinition[] cups =
            [new("Copa Oro", FromPosition: 1, ToPosition: 4, BestOf: 3)];
        SampleTournamentBuilder.TournamentDefinition definition = MakeDefinition(4, cups);

        int playerCounter = 0;
        SampleTournamentBuilder.BuildResult result =
            SampleTournamentBuilder.Build(definition, BuildVenues(), ref playerCounter, includePlayoffs: true);

        Division division = result.Tournament.Divisions.Single();
        List<Stage> cupStages = [.. division.Stages.Where(s => s.BracketName == "Copa Oro")];

        Assert.Equal(2, cupStages.Count);
        Assert.Contains(cupStages, s => s.StageType == StageType.SemiFinal);
        Assert.Contains(cupStages, s => s.StageType == StageType.Final);
        Assert.All(cupStages, s => Assert.Equal(3, s.BestOf));

        Stage final = cupStages.Single(s => s.StageType == StageType.Final);
        Match finalMatch = Assert.Single(final.Matches);
        Assert.True(finalMatch.IsFinished);
        Assert.NotNull(finalMatch.WinningTeamId);
    }
}
