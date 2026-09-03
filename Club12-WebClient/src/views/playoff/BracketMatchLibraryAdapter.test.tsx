import { render } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { IMatchResponse } from '@/modules/match/type/match.d';
import type { LibraryMatchComponentProps } from '@/modules/playoff/type/gLootBracketTypes.d';
import BracketMatchLibraryAdapter from './BracketMatchLibraryAdapter';

const guid = (seed: string): GUID => `${seed}-0000-0000-0000-000000000000` as GUID;

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

const asLibraryMatch = (raw: IMatchResponse): LibraryMatchComponentProps['match'] =>
  ({ id: raw.id, nextMatchId: null, raw, participants: [] }) as unknown as LibraryMatchComponentProps['match'];

describe('BracketMatchLibraryAdapter', () => {
  it('renders nothing for a decided bye, leaving its slot in the SVG empty', () => {
    // Regression: the library positions every OTHER card, and every
    // connector line, from this match's row/column index — the slot must
    // stay in the bracket's match array (see bracketAdapter.test.ts), only
    // its visible content is suppressed, here.
    const byeMatch: IMatchResponse = {
      ...baseMatch,
      isFinished: true,
      winningTeamId: baseMatch.homeTeam!.id,
      winningTeamName: 'Halcones',
    };

    const { container } = render(
      <BracketMatchLibraryAdapter match={asLibraryMatch(byeMatch)} />
    );

    expect(container).toBeEmptyDOMElement();
  });

  it('renders the match card for a real, decided match', () => {
    const decidedMatch: IMatchResponse = {
      ...baseMatch,
      visitorTeam: { id: guid('visitor'), name: 'Cóndores', logoUrl: '', score: 70, players: [], scorers: [] },
      isFinished: true,
      winningTeamId: baseMatch.homeTeam!.id,
      winningTeamName: 'Halcones',
    };

    const { container } = render(
      <BracketMatchLibraryAdapter match={asLibraryMatch(decidedMatch)} />
    );

    expect(container).not.toBeEmptyDOMElement();
  });
});
