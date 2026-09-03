import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
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

    // The series-backed pairing (Black Mamba/NTI) shows each side's
    // per-game scores, not the 119/101 aggregate; the plain match
    // (Halcones/Cóndores, no series in seriesById) still shows its one
    // final score as before.
    expect(screen.getByText('60')).toBeInTheDocument();
    expect(screen.getByText('59')).toBeInTheDocument();
    expect(screen.getByText('55')).toBeInTheDocument();
    expect(screen.getByText('46')).toBeInTheDocument();
    expect(screen.queryByText('119')).not.toBeInTheDocument();
    expect(screen.queryByText('101')).not.toBeInTheDocument();
    expect(screen.getByText('90')).toBeInTheDocument();
    expect(screen.getByText('80')).toBeInTheDocument();

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

  it('clicking a BestOf>1 series card navigates to its next unfinished game, never the synthetic series id', async () => {
    const sfStage = guid('sf');
    const series = guid('series-1');

    const seriesMatch: IMatchResponse = {
      id: series,
      matchDate: '2026-01-01T18:00:00Z',
      matchType: 'Playoff' as IMatchResponse['matchType'],
      slug: '',
      homeTeam: { id: guid('black-mamba'), name: 'Black Mamba', logoUrl: '', score: 60, players: [], scorers: [] },
      visitorTeam: { id: guid('nti'), name: 'NTI', logoUrl: '', score: 55, players: [], scorers: [] },
      isFinished: false,
      winningTeamId: null,
      winningTeamName: null,
      venue: null,
      stageId: sfStage,
    };

    const model: BracketModel = {
      rounds: [{ stageId: sfStage, stageType: StageType.SemiFinal, matches: [seriesMatch] }],
      edges: [],
    };

    const nextGame = guid('g2');
    const seriesById = new Map<GUID, IMatchSeriesResponse>([
      [
        series,
        {
          id: series,
          stageId: sfStage,
          homeTeamId: guid('black-mamba'),
          homeTeamName: 'Black Mamba',
          visitorTeamId: guid('nti'),
          visitorTeamName: 'NTI',
          bestOf: 3,
          winningTeamId: null,
          winningTeamName: null,
          games: [
            makeGame({ id: guid('g1'), gameNumber: 1, isFinished: true, homeScore: 60, visitorScore: 55, winningTeamName: 'Black Mamba' }),
            makeGame({ id: nextGame, gameNumber: 2, isFinished: false }),
          ],
        },
      ],
    ]);

    const onMatchClick = vi.fn();
    render(<PlayoffBracket model={model} seriesById={seriesById} onMatchClick={onMatchClick} />);

    await userEvent.click(screen.getByText('Black Mamba'));

    expect(onMatchClick).toHaveBeenCalledWith(nextGame);
    expect(onMatchClick).not.toHaveBeenCalledWith(series);
  });

  it('does not attach a click handler to a still-TBD slot', async () => {
    const finalStage = guid('final');
    const tbdMatch: IMatchResponse = {
      id: guid('m-final'),
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
      rounds: [{ stageId: finalStage, stageType: StageType.Final, matches: [tbdMatch] }],
      edges: [],
    };

    const onMatchClick = vi.fn();
    render(<PlayoffBracket model={model} onMatchClick={onMatchClick} />);

    await userEvent.click(screen.getAllByText('A definir')[0]);

    expect(onMatchClick).not.toHaveBeenCalled();
  });

  it('hides the dangling connector into a decided bye sibling instead of drawing a line to nowhere', () => {
    // Regression: the library draws every match's top/bottom connector
    // purely from row position, independent of whether that row actually
    // has a rendered card — hiding a bye's card (BracketMatchLibraryAdapter)
    // without also suppressing its connector left a line hanging in empty
    // space with nothing at its far end.
    const qfStage = guid('qf');
    const sfStage = guid('sf');

    const byeMatch: IMatchResponse = {
      id: guid('m-bye'),
      matchDate: '2026-01-01T18:00:00Z',
      matchType: 'Playoff' as IMatchResponse['matchType'],
      slug: '',
      homeTeam: { id: guid('team-a'), name: 'Equipo A', logoUrl: '', score: 0, players: [], scorers: [] },
      visitorTeam: null,
      isFinished: true,
      winningTeamId: guid('team-a'),
      winningTeamName: 'Equipo A',
      venue: null,
      stageId: qfStage,
    };
    const realMatch: IMatchResponse = {
      id: guid('m-real'),
      matchDate: '2026-01-01T18:00:00Z',
      matchType: 'Playoff' as IMatchResponse['matchType'],
      slug: '',
      homeTeam: { id: guid('team-b'), name: 'Equipo B', logoUrl: '', score: 80, players: [], scorers: [] },
      visitorTeam: { id: guid('team-c'), name: 'Equipo C', logoUrl: '', score: 70, players: [], scorers: [] },
      isFinished: true,
      winningTeamId: guid('team-b'),
      winningTeamName: 'Equipo B',
      venue: null,
      stageId: qfStage,
    };
    const sfMatch: IMatchResponse = {
      id: guid('m-sf'),
      matchDate: '2026-01-08T18:00:00Z',
      matchType: 'Playoff' as IMatchResponse['matchType'],
      slug: '',
      homeTeam: null,
      visitorTeam: null,
      isFinished: false,
      winningTeamId: null,
      winningTeamName: null,
      venue: null,
      stageId: sfStage,
    };

    const model: BracketModel = {
      rounds: [
        { stageId: qfStage, stageType: StageType.QuarterFinal, matches: [byeMatch, realMatch] },
        { stageId: sfStage, stageType: StageType.SemiFinal, matches: [sfMatch] },
      ],
      edges: [],
    };

    const { container } = render(<PlayoffBracket model={model} />);

    const childCard = container.querySelector(`[data-match-id="${sfMatch.id}"]`);
    const outerCell = childCard?.closest('svg')?.parentElement?.parentElement;
    const connectorPaths = outerCell?.querySelectorAll<SVGPathElement>('path[id^="connector-"]');

    expect(connectorPaths).toHaveLength(2);
    // byeMatch is the first sibling (top source) — its connector is hidden;
    // realMatch is the second (bottom source) — its connector stays visible.
    expect(connectorPaths?.[0].style.display).toBe('none');
    expect(connectorPaths?.[1].style.display).not.toBe('none');
  });
});
