import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { IMatchResponse } from '@/modules/match/type/match.d';
import BracketMatchNode from './BracketMatchNode';

const guid = (seed: string): GUID => `${seed}-0000-0000-0000-000000000000` as GUID;

const baseMatch: IMatchResponse = {
  id: guid('match'),
  matchDate: '2026-01-01T18:00:00Z',
  matchType: 'Playoff' as IMatchResponse['matchType'],
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
});
