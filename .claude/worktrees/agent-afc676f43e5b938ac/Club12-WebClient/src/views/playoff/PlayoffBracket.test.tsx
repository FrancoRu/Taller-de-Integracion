import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { IMatchSeriesResponse, ISeriesGameResponse } from '@/modules/matchSeries/type/matchSeries.d';
import { StageType } from '@/modules/stage/type/stage';
import { BracketModel } from '@/modules/playoff/type/bracket.d';
import PlayoffBracket from './PlayoffBracket';

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

describe('PlayoffBracket', () => {
  it('renders a two-legged (ida/vuelta) semifinal series as one node per pairing, not one per game', () => {
    const sfStage = guid('sf');
    const finalStage = guid('final');

    const seriesOne = guid('series-1');
    const seriesTwo = guid('series-2');
    const finalMatch = guid('m-final');

    const seriesOneMatch: IMatchResponse = {
      id: seriesOne,
      matchDate: '2026-01-01T18:00:00Z',
      matchType: 'Playoff' as IMatchResponse['matchType'],
      slug: '',
      homeTeam: { id: guid('black-mamba'), name: 'Black Mamba', logoUrl: '', score: 119, players: [], scorers: [] },
      visitorTeam: { id: guid('nti'), name: 'NTI', logoUrl: '', score: 101, players: [], scorers: [] },
      isFinished: true,
      winningTeamId: guid('black-mamba'),
      winningTeamName: 'Black Mamba',
      venue: null,
      stageId: sfStage,
    };
    const seriesTwoMatch: IMatchResponse = {
      id: seriesTwo,
      matchDate: '2026-01-01T18:00:00Z',
      matchType: 'Playoff' as IMatchResponse['matchType'],
      slug: '',
      homeTeam: { id: guid('halcones'), name: 'Halcones', logoUrl: '', score: 90, players: [], scorers: [] },
      visitorTeam: { id: guid('condores'), name: 'Cóndores', logoUrl: '', score: 80, players: [], scorers: [] },
      isFinished: true,
      winningTeamId: guid('halcones'),
      winningTeamName: 'Halcones',
      venue: null,
      stageId: sfStage,
    };
    const finalMatchRow: IMatchResponse = {
      id: finalMatch,
      matchDate: '2026-01-08T18:00:00Z',
      matchType: 'Playoff' as IMatchResponse['matchType'],
      slug: '',
      homeTeam: null,
      visitorTeam: null,
      isFinished: false,
      winningTeamId: null,
      winningTeamName: null,
      venue: null,
      stageId: finalStage,
    };

    const model: BracketModel = {
      rounds: [
        {
          stageId: sfStage,
          stageType: StageType.SemiFinal,
          matches: [seriesOneMatch, seriesTwoMatch],
        },
        { stageId: finalStage, stageType: StageType.Final, matches: [finalMatchRow] },
      ],
      edges: [],
    };

    const seriesById = new Map<GUID, IMatchSeriesResponse>([
      [
        seriesOne,
        {
          id: seriesOne,
          stageId: sfStage,
          homeTeamId: guid('black-mamba'),
          homeTeamName: 'Black Mamba',
          visitorTeamId: guid('nti'),
          visitorTeamName: 'NTI',
          bestOf: 2,
          winningTeamId: guid('black-mamba'),
          winningTeamName: 'Black Mamba',
          games: [
            makeGame({ id: guid('g1'), gameNumber: 1, isFinished: true, homeScore: 60, visitorScore: 55, winningTeamName: 'Black Mamba' }),
            makeGame({ id: guid('g2'), gameNumber: 2, isFinished: true, homeScore: 59, visitorScore: 46, winningTeamName: 'Black Mamba' }),
          ],
        },
      ],
    ]);

    render(<PlayoffBracket model={model} seriesById={seriesById} />);

    // Exactly one card per series/pairing — not one card per individual leg.
    expect(screen.getAllByText('Black Mamba')).toHaveLength(1);
    expect(screen.getAllByText('NTI')).toHaveLength(1);
    expect(screen.getByText('Halcones')).toBeInTheDocument();
    expect(screen.getByText('Cóndores')).toBeInTheDocument();

    // Aggregate score, not the individual leg score.
    expect(screen.getByText('119')).toBeInTheDocument();
    expect(screen.getByText('101')).toBeInTheDocument();

    // The Final's TBD slots are still present (SF winners not the whole model yet).
    expect(screen.getAllByText('A definir')).toHaveLength(2);
  });

  it('still renders every semifinal card when the Final round has zero match rows (e.g. not yet generated)', () => {
    // Regression test for a real bug found via live/manual verification:
    // @g-loot/react-tournament-brackets only renders one match when more
    // than one match in the tree has nextMatchId: null. Before the fix,
    // an empty trailing round caused every semifinal match to become an
    // untethered "root", and the library silently dropped all but one.
    const sfStage = guid('sf');
    const finalStage = guid('final');

    const seriesOneMatch: IMatchResponse = {
      id: guid('series-1'),
      matchDate: '2026-01-01T18:00:00Z',
      matchType: 'Playoff' as IMatchResponse['matchType'],
      slug: '',
      homeTeam: { id: guid('black-mamba'), name: 'Black Mamba', logoUrl: '', score: 1, players: [], scorers: [] },
      visitorTeam: { id: guid('nti'), name: 'NTI', logoUrl: '', score: 0, players: [], scorers: [] },
      isFinished: true,
      winningTeamId: guid('black-mamba'),
      winningTeamName: 'Black Mamba',
      venue: null,
      stageId: sfStage,
    };
    const seriesTwoMatch: IMatchResponse = {
      id: guid('series-2'),
      matchDate: '2026-01-01T18:00:00Z',
      matchType: 'Playoff' as IMatchResponse['matchType'],
      slug: '',
      homeTeam: { id: guid('halcones'), name: 'Halcones', logoUrl: '', score: 1, players: [], scorers: [] },
      visitorTeam: { id: guid('condores'), name: 'Cóndores', logoUrl: '', score: 0, players: [], scorers: [] },
      isFinished: true,
      winningTeamId: guid('halcones'),
      winningTeamName: 'Halcones',
      venue: null,
      stageId: sfStage,
    };

    const model: BracketModel = {
      rounds: [
        { stageId: sfStage, stageType: StageType.SemiFinal, matches: [seriesOneMatch, seriesTwoMatch] },
        { stageId: finalStage, stageType: StageType.Final, matches: [] },
      ],
      edges: [],
    };

    render(<PlayoffBracket model={model} />);

    expect(screen.getByText('Black Mamba')).toBeInTheDocument();
    expect(screen.getByText('Halcones')).toBeInTheDocument();
  });
});
