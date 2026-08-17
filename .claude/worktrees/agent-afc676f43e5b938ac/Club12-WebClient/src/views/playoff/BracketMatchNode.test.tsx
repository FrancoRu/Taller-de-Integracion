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

  it('shows the aggregate score and a one-line per-game summary for a two-legged series, with no collapse toggle', () => {
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

    expect(screen.getByText('Al mejor de 2')).toBeInTheDocument();
    expect(screen.getByText('119')).toBeInTheDocument();
    expect(screen.getByText('101')).toBeInTheDocument();
    expect(screen.getByText('J1 60-55 · J2 59-46')).toBeInTheDocument();
    expect(screen.queryByRole('button')).not.toBeInTheDocument();
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
