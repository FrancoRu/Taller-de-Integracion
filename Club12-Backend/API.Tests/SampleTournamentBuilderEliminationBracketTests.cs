using Application.Utils.Helper.Series;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using System.Linq;

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
/// always play Bo1. A BestOf > 1 round is a REAL MatchSeries with as many
/// finished games as it took to decide it — not one collapsed match.
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

        // 9 real seeds padded to 16 = 7 byes + 1 real match in round 1. Every
        // one of the 8 pairings gets a match row: a bye is a finished match with
        // no visitor and no score whose home team is the winner — the same shape
        // StageService.FillStageWithSeedsAsync writes — so the stage always has
        // slots for the teams assigned to it.
        Assert.Equal(9, roundOf16.StageTeamMatches.Count);
        Assert.Equal(8, roundOf16.Matches.Count);

        List<Match> roundOf16Byes = [.. roundOf16.Matches.Where(m => m.VisitorTeamId is null)];
        Assert.Equal(7, roundOf16Byes.Count);
        Assert.All(roundOf16Byes, m =>
        {
            Assert.True(m.IsFinished);
            Assert.Null(m.HomeScore);
            Assert.Null(m.VisitorScore);
            Assert.Equal(m.HomeTeamId, m.WinningTeamId);
        });

        // Every later round is bye-free: the 8 round-1 winners (7 byes plus
        // the one real match's winner) actually play on, halving each round
        // down to the champion.
        Assert.Equal(8, quarterFinal.StageTeamMatches.Count);
        Assert.Equal(4, quarterFinal.Matches.Count);
        Assert.Equal(4, semiFinal.StageTeamMatches.Count);
        Assert.Equal(2, final.StageTeamMatches.Count);

        // Series length applies only to the last two rounds.
        // Earlier rounds are always single games.
        Assert.Equal(1, roundOf16.BestOf);
        Assert.Equal(1, quarterFinal.BestOf);
        Assert.Equal(3, semiFinal.BestOf);
        Assert.Equal(3, final.BestOf);

        // SemiFinal (2 pairings) and Final (1 pairing) are BestOf=3: each
        // pairing is a REAL MatchSeries — SeriesDecisionCalculator decides
        // it, not one collapsed match — with between 2 (a sweep) and 3 (the
        // full distance) finished games, and every one of those games also
        // belongs to the stage's Matches (so total match counts include
        // every game actually played).
        Assert.Equal(2, semiFinal.MatchSeries.Count);
        Assert.All(semiFinal.MatchSeries, AssertDecidedSeries);
        Assert.Equal(semiFinal.MatchSeries.Sum(s => s.Matches.Count), semiFinal.Matches.Count);

        MatchSeries finalSeries = Assert.Single(final.MatchSeries);
        AssertDecidedSeries(finalSeries);
        Assert.Equal(finalSeries.Matches.Count, final.Matches.Count);
    }

    /// <summary>
    /// A decided best-of-N series: SeriesDecisionCalculator.DetermineWinner
    /// agrees with the recorded WinningTeamId (single source of truth), and
    /// the number of finished games generated is between the minimum needed
    /// to clinch it and the series' full BestOf — never more (no games are
    /// generated past the decisive one) and never less (undecided).
    /// </summary>
    private static void AssertDecidedSeries(MatchSeries series)
    {
        Assert.NotNull(series.WinningTeamId);
        Assert.Equal(series.WinningTeamId, SeriesDecisionCalculator.DetermineWinner(series));

        int gamesToWin = (series.BestOf / 2) + 1;
        Assert.InRange(series.Matches.Count, gamesToWin, series.BestOf);
        Assert.All(series.Matches, m => Assert.True(m.IsFinished));
        Assert.All(series.Matches, m => Assert.NotNull(m.GameNumber));
        Assert.Equal(
            Enumerable.Range(1, series.Matches.Count),
            series.Matches.OrderBy(m => m.GameNumber).Select(m => m.GameNumber!.Value));
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
        MatchSeries finalSeries = Assert.Single(final.MatchSeries);
        AssertDecidedSeries(finalSeries);
        Assert.Equal(finalSeries.Matches.Count, final.Matches.Count);
    }

    /// <summary>
    /// BestOf=1 stays exactly how every pre-existing elimination stage in
    /// this builder already works (see Stage.BestOf's doc comment: "1 means
    /// a single match decides the round") — one plain, finished, decided
    /// Match per pairing, with NO MatchSeries created. Only BestOf>1 rounds
    /// generate a real series.
    /// </summary>
    [Fact]
    public void Build_CupWithBestOfOne_UsesAPlainSingleMatchNotASeries()
    {
        SampleTournamentBuilder.PlayoffCupDefinition[] cups =
            [new("Copa Bronce", FromPosition: 1, ToPosition: 4, BestOf: 1)];
        SampleTournamentBuilder.TournamentDefinition definition = MakeDefinition(4, cups);

        int playerCounter = 0;
        SampleTournamentBuilder.BuildResult result =
            SampleTournamentBuilder.Build(definition, BuildVenues(), ref playerCounter, includePlayoffs: true);

        Division division = result.Tournament.Divisions.Single();
        Stage final = division.Stages.Single(s => s.BracketName == "Copa Bronce" && s.StageType == StageType.Final);

        Assert.Equal(1, final.BestOf);
        Assert.Empty(final.MatchSeries);
        Match finalMatch = Assert.Single(final.Matches);
        Assert.True(finalMatch.IsFinished);
        Assert.NotNull(finalMatch.WinningTeamId);
        Assert.Null(finalMatch.SeriesId);
        Assert.Null(finalMatch.GameNumber);
    }

    /// <summary>
    /// The generated game count is not fixed per BestOf — a batch of several
    /// Bo5 series (3 cups x SemiFinal+Final pairings, 9 series total) mixes
    /// sweeps (3 games), 4-game series, and full-distance 5-game series,
    /// instead of every series ending identically. Determinism (same input,
    /// same output every run — no Random/DateTime) is implicit: this test
    /// itself passing reliably (not flaking) across runs proves it.
    /// </summary>
    [Fact]
    public void Build_SeveralBestOfFiveSeries_ProducesVariedGameCounts()
    {
        SampleTournamentBuilder.PlayoffCupDefinition[] cups =
        [
            new("Copa A", FromPosition: 1, ToPosition: 4, BestOf: 5),
            new("Copa B", FromPosition: 5, ToPosition: 8, BestOf: 5),
            new("Copa C", FromPosition: 9, ToPosition: 12, BestOf: 5),
        ];
        SampleTournamentBuilder.TournamentDefinition definition = MakeDefinition(12, cups);

        int playerCounter = 0;
        SampleTournamentBuilder.BuildResult result =
            SampleTournamentBuilder.Build(definition, BuildVenues(), ref playerCounter, includePlayoffs: true);

        Division division = result.Tournament.Divisions.Single();
        List<MatchSeries> allSeries = [.. division.Stages.SelectMany(s => s.MatchSeries)];

        // 3 cups x (2 SemiFinal pairings + 1 Final pairing) = 9 series.
        Assert.Equal(9, allSeries.Count);
        Assert.All(allSeries, AssertDecidedSeries);

        List<int> gameCounts = [.. allSeries.Select(s => s.Matches.Count)];
        Assert.True(
            gameCounts.Distinct().Count() > 1,
            $"Expected varied game counts across the batch, got: [{string.Join(", ", gameCounts)}]");
    }
}
