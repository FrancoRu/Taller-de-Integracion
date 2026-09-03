import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { IMatchSeriesResponse, ISeriesGameResponse } from '@/modules/matchSeries/type/matchSeries.d';
import BracketMatchNode from './BracketMatchNode';

const guid = (seed: string): GUID => `${seed}-0000-0000-0000-000000000000` as GUID;

const makeGame = (
  overrides: Partial<ISeriesGameResponse> & { id: GUID }
): ISeriesGameResponse => ({
  matchDate: '2026-01-01T18:00:00Z',
  homeTeamName: 'Black Mamba',
  visitorTeamName: 'NTI',
  homeScore: null,
  visitorScore: null,
  winningTeamName: null,
  isFinished: false,
  matchType: 'Playoff' as ISeriesGameResponse['matchType'],
  gameNumber: 1,
  ...overrides,
});

const makeSeries = (
  overrides: Partial<IMatchSeriesResponse> & { id: GUID; stageId: GUID }
): IMatchSeriesResponse => ({
  homeTeamId: guid('home'),
  homeTeamName: 'Black Mamba',
  visitorTeamId: guid('visitor'),
  visitorTeamName: 'NTI',
  bestOf: 2,
  winningTeamId: null,
  winningTeamName: null,
  games: [],
  ...overrides,
});

const baseMatch: IMatchResponse = {
  id: guid('match'),
  matchDate: '2026-01-01T18:00:00Z',
  matchType: 'Playoff' as IMatchResponse['matchType'],
  slug: 'halcones-vs-tbd-2026-01-01',
  homeTeam: { id: guid('home'), name: 'Halcones', logoUrl: '', score: 0, players: [], scorers: [] },
  visitorTeam: null,
  isFinished: false,
  winningTeamId: null,
  winningTeamName: null,
  venue: null,
  stageId: guid('stage'),
};

describe('BracketMatchNode', () => {
  it('shows "A definir" for a slot still waiting on a previous round winner', () => {
    render(<BracketMatchNode match={baseMatch} />);

    expect(screen.getByText('A definir')).toBeInTheDocument();
  });

  it('shows "BYE" once a walkover has already been decided with only one side ever assigned', () => {
    const byeMatch: IMatchResponse = {
      ...baseMatch,
      isFinished: true,
      winningTeamId: baseMatch.homeTeam!.id,
      winningTeamName: 'Halcones',
    };

    render(<BracketMatchNode match={byeMatch} />);

    expect(screen.getByText('BYE')).toBeInTheDocument();
    expect(screen.queryByText('A definir')).not.toBeInTheDocument();
  });

  it('does not call a finished match with both sides present a bye', () => {
    const decidedMatch: IMatchResponse = {
      ...baseMatch,
      visitorTeam: { id: guid('visitor'), name: 'Cóndores', logoUrl: '', score: 70, players: [], scorers: [] },
      isFinished: true,
      winningTeamId: baseMatch.homeTeam!.id,
      winningTeamName: 'Halcones',
    };

    render(<BracketMatchNode match={decidedMatch} />);

    expect(screen.queryByText('BYE')).not.toBeInTheDocument();
    expect(screen.getByText('Cóndores')).toBeInTheDocument();
  });

  it('shows a "BOn" format badge and each side\'s per-game scores for a best-of-N series, not just the aggregate', () => {
    const homeId = guid('home');
    const visitorId = guid('visitor');
    const seriesMatch: IMatchResponse = {
      ...baseMatch,
      homeTeam: { id: homeId, name: 'Black Mamba', logoUrl: '', score: 119, players: [], scorers: [] },
      visitorTeam: { id: visitorId, name: 'NTI', logoUrl: '', score: 101, players: [], scorers: [] },
      isFinished: true,
      winningTeamId: homeId,
      winningTeamName: 'Black Mamba',
    };
    const series = makeSeries({
      id: seriesMatch.id,
      stageId: baseMatch.stageId!,
      bestOf: 2,
      games: [
        makeGame({ id: guid('g1'), gameNumber: 1, isFinished: true, homeScore: 60, visitorScore: 55, winningTeamName: 'Black Mamba' }),
        makeGame({ id: guid('g2'), gameNumber: 2, isFinished: true, homeScore: 59, visitorScore: 46, winningTeamName: 'Black Mamba' }),
      ],
    });

    render(<BracketMatchNode match={seriesMatch} series={series} />);

    expect(screen.getByText('BO2')).toBeInTheDocument();
    // Per-game scores for each side, game 1 then game 2 — not the 119/101 aggregate.
    expect(screen.getByText('60')).toBeInTheDocument();
    expect(screen.getByText('59')).toBeInTheDocument();
    expect(screen.getByText('55')).toBeInTheDocument();
    expect(screen.getByText('46')).toBeInTheDocument();
    expect(screen.queryByText('119')).not.toBeInTheDocument();
    expect(screen.queryByText('101')).not.toBeInTheDocument();
    expect(screen.queryByRole('button')).not.toBeInTheDocument();
  });

  it('only shows finished games, skipping an in-progress series\' not-yet-played game', () => {
    const homeId = guid('home');
    const visitorId = guid('visitor');
    const seriesMatch: IMatchResponse = {
      ...baseMatch,
      homeTeam: { id: homeId, name: 'Black Mamba', logoUrl: '', score: 1, players: [], scorers: [] },
      visitorTeam: { id: visitorId, name: 'NTI', logoUrl: '', score: 0, players: [], scorers: [] },
      isFinished: false,
      winningTeamId: null,
      winningTeamName: null,
    };
    const series = makeSeries({
      id: seriesMatch.id,
      stageId: baseMatch.stageId!,
      bestOf: 3,
      games: [
        makeGame({ id: guid('g1'), gameNumber: 1, isFinished: true, homeScore: 60, visitorScore: 55, winningTeamName: 'Black Mamba' }),
        makeGame({ id: guid('g2'), gameNumber: 2, isFinished: false }),
      ],
    });

    render(<BracketMatchNode match={seriesMatch} series={series} />);

    expect(screen.getByText('60')).toBeInTheDocument();
    expect(screen.getByText('55')).toBeInTheDocument();
    expect(screen.queryAllByText('0')).toHaveLength(0);
  });

  it('shows an "IV" (ida y vuelta) badge and each side\'s per-leg scores for a client-inferred two-leg tie (no MatchSeries)', () => {
    const homeId = guid('home');
    const visitorId = guid('visitor');
    const tieMatch: IMatchResponse = {
      ...baseMatch,
      homeTeam: { id: homeId, name: '2K', logoUrl: '', score: 98, players: [], scorers: [] },
      visitorTeam: { id: visitorId, name: 'NN', logoUrl: '', score: 118, players: [], scorers: [] },
      isFinished: true,
      winningTeamId: visitorId,
      winningTeamName: 'NN',
    };
    const legs: IMatchResponse[] = [
      {
        ...baseMatch,
        id: guid('leg1'),
        homeTeam: { id: homeId, name: '2K', logoUrl: '', score: 41, players: [], scorers: [] },
        visitorTeam: { id: visitorId, name: 'NN', logoUrl: '', score: 64, players: [], scorers: [] },
        isFinished: true,
        winningTeamId: visitorId,
        winningTeamName: 'NN',
      },
      {
        ...baseMatch,
        id: guid('leg2'),
        homeTeam: { id: visitorId, name: 'NN', logoUrl: '', score: 54, players: [], scorers: [] },
        visitorTeam: { id: homeId, name: '2K', logoUrl: '', score: 57, players: [], scorers: [] },
        isFinished: true,
        winningTeamId: homeId,
        winningTeamName: '2K',
      },
    ];

    render(<BracketMatchNode match={tieMatch} legs={legs} />);

    expect(screen.getByText('IV')).toBeInTheDocument();
    // 2K's per-leg scores (41 at home, then 57 away) and NN's (64 away, then 54 at home).
    expect(screen.getByText('41')).toBeInTheDocument();
    expect(screen.getByText('57')).toBeInTheDocument();
    expect(screen.getByText('64')).toBeInTheDocument();
    expect(screen.getByText('54')).toBeInTheDocument();
    expect(screen.queryByText('98')).not.toBeInTheDocument();
    expect(screen.queryByText('118')).not.toBeInTheDocument();
  });

  it('shows no format badge for the normal single-match case (no legs, no series)', () => {
    const decidedMatch: IMatchResponse = {
      ...baseMatch,
      visitorTeam: { id: guid('visitor'), name: 'Cóndores', logoUrl: '', score: 70, players: [], scorers: [] },
      isFinished: true,
      winningTeamId: baseMatch.homeTeam!.id,
      winningTeamName: 'Halcones',
    };

    render(<BracketMatchNode match={decidedMatch} />);

    expect(screen.queryByText('IV')).not.toBeInTheDocument();
    expect(screen.queryByText(/^BO/)).not.toBeInTheDocument();
  });

  it("renders each side's TeamLogo", () => {
    const decidedMatch: IMatchResponse = {
      ...baseMatch,
      homeTeam: { ...baseMatch.homeTeam!, logoUrl: 'https://example.com/halcones.png' },
      visitorTeam: {
        id: guid('visitor'),
        name: 'Cóndores',
        logoUrl: 'https://example.com/condores.png',
        score: 70,
        players: [],
        scorers: [],
      },
    };

    render(<BracketMatchNode match={decidedMatch} />);

    expect(screen.getByAltText('Logo de Halcones')).toBeInTheDocument();
    expect(screen.getByAltText('Logo de Cóndores')).toBeInTheDocument();
  });
});
