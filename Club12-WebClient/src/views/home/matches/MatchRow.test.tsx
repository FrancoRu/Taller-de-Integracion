import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { MatchType } from '@/modules/core/enum/match/matchType';
import { IMatchResponse } from '@/modules/match/type/match';
import { ITeamMatchResponse } from '@/modules/team/type/team';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import MatchRow from '@/views/home/matches/MatchRow';

const guid = (value: string) => value as GUID;

const team = (name: string): ITeamMatchResponse => ({
  id: guid(`team-${name}`),
  name,
  logoUrl: '',
  score: 0,
  players: [],
  scorers: [],
});

const match: IMatchResponse = {
  id: guid('match-1'),
  matchDate: '2026-04-28T20:00:00Z',
  round: 1,
  matchType: MatchType.Regular,
  slug: 'a-vs-b',
  homeTeam: team('A'),
  visitorTeam: team('B'),
  isFinished: false,
  winningTeamId: null,
  venue: null,
  stageId: guid('stage-1'),
  winningTeamName: null,
  status: null,
};

describe('MatchRow', () => {
  it('links to the public match page by default', () => {
    render(
      <MemoryRouter>
        <MatchRow match={match} />
      </MemoryRouter>
    );

    expect(screen.getByRole('link')).toHaveAttribute(
      'href',
      APP_ROUTES.publicMatch.build('a-vs-b')
    );
  });

  it('shows both the date and the time, not just the time', () => {
    render(
      <MemoryRouter>
        <MatchRow match={match} />
      </MemoryRouter>
    );

    expect(screen.getByText('28/04/2026')).toBeInTheDocument();
    expect(screen.getByText('17:00')).toBeInTheDocument();
  });

  it('links to the admin panel match page when buildHref targets it', () => {
    render(
      <MemoryRouter>
        <MatchRow match={match} buildHref={m => APP_ROUTES.panelMatch.build(m.slug ?? m.id)} />
      </MemoryRouter>
    );

    const href = screen.getByRole('link').getAttribute('href');
    expect(href).toBe(APP_ROUTES.panelMatch.build('a-vs-b'));
    expect(href).toMatch(/^\/panel\/partidos\//);
  });
});
