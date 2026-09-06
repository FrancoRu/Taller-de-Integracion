import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { StageType, IStageResponse } from '@/modules/stage/type/stage';
import { BracketGroup } from '@/modules/playoff/type/bracket.d';
import { DivisionFixtureSection } from '@/modules/match/utils/divisionFixtureSections';
import PlayoffCups from '@/views/playoff/PlayoffCups';

const guid = (value: string) => value as GUID;

const team = (name: string) => ({
  id: guid(`team-${name}`),
  name,
  logoUrl: '',
  score: 1,
  players: [],
  scorers: [],
});

let sequence = 0;
const match = (stageId: string, home: string, visitor: string): IMatchResponse => ({
  id: guid(`match-${(sequence += 1)}`),
  matchDate: '2026-04-28T20:00:00Z',
  round: null,
  matchType: 'Playoff' as IMatchResponse['matchType'],
  slug: `match-${sequence}`,
  homeTeam: team(home),
  visitorTeam: team(visitor),
  isFinished: false,
  winningTeamId: null,
  venue: null,
  stageId: guid(stageId),
  winningTeamName: null,
  status: null,
});

const stage = (id: string, bracketName: string | null): IStageResponse => ({
  id: guid(id),
  slug: `stage-${id}`,
  name: `Semifinal ${id}`,
  description: null,
  stageType: StageType.SemiFinal,
  isActive: true,
  isElimination: true,
  startDate: '2026-04-01T00:00:00Z',
  endDate: '2026-05-01T00:00:00Z',
  divisionId: guid('division-1'),
  order: 0,
  bracketName,
  bestOf: 1,
  roundRobinLegs: 1,
});

const bracketGroup = (
  bracketName: string | null,
  stageId: string,
  m: IMatchResponse[],
  drawnAt: string | null = null
): BracketGroup => ({
  bracketName,
  model: {
    rounds: [{ stageId: guid(stageId), stageType: StageType.SemiFinal, matches: m }],
    edges: [],
  },
  drawnAt,
});

const fixtureSection = (bracketName: string | null, stageId: string, m: IMatchResponse[]): DivisionFixtureSection => ({
  stage: stage(stageId, bracketName),
  label: 'Semifinal',
  matches: m,
});

describe('PlayoffCups', () => {
  it('renders a single, unnamed bracket with no tier accordion and one CSV export', () => {
    const m = [match('semi', 'A', 'B')];
    const groups = [bracketGroup(null, 'semi', m)];
    const sections = [fixtureSection(null, 'semi', m)];

    render(
      <MemoryRouter>
        <PlayoffCups bracketGroups={groups} matchSections={sections} />
      </MemoryRouter>
    );

    expect(screen.getAllByText('A').length).toBeGreaterThan(0);
    expect(screen.queryByRole('button', { name: /expandir|expand/i })).not.toBeInTheDocument();
    expect(screen.getAllByText('Exportar CSV')).toHaveLength(1);
  });

  it('shows every cup open by default (top cup prominent, lower cups collapsible but not hidden), one CSV export each', async () => {
    const oroMatches = [match('oro-semi', 'Echagüe', 'Neptunia')];
    const plataMatches = [match('plata-semi', 'Bovril', 'Viale')];
    const groups = [
      bracketGroup('Copa Oro', 'oro-semi', oroMatches),
      bracketGroup('Copa Plata', 'plata-semi', plataMatches),
    ];
    const sections = [
      fixtureSection('Copa Oro', 'oro-semi', oroMatches),
      fixtureSection('Copa Plata', 'plata-semi', plataMatches),
    ];

    render(
      <MemoryRouter>
        <PlayoffCups bracketGroups={groups} matchSections={sections} />
      </MemoryRouter>
    );

    // Both cups' matches are visible immediately, no interaction needed —
    // Copa Plata is de-emphasized (smaller, muted heading), not hidden.
    expect(screen.getAllByText('Echagüe').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Bovril').length).toBeGreaterThan(0);
    const plataToggle = screen.getByText('Copa Plata');
    expect(plataToggle.closest('[aria-expanded]')).toHaveAttribute('aria-expanded', 'true');

    // Still collapsible on demand, for a reader who wants to declutter.
    await userEvent.click(plataToggle);
    expect(plataToggle.closest('[aria-expanded]')).toHaveAttribute('aria-expanded', 'false');

    // One export button per cup — not one per round within a cup.
    expect(screen.getAllByText('Exportar CSV')).toHaveLength(2);
  });

  // Playoff draw & seeding: the public "Sorteo realizado" transparency label.
  it('shows "Sorteo realizado el [fecha]" when the bracket has a drawnAt', () => {
    const m = [match('semi', 'A', 'B')];
    const groups = [bracketGroup(null, 'semi', m, '2026-05-01T12:00:00Z')];
    const sections = [fixtureSection(null, 'semi', m)];

    render(
      <MemoryRouter>
        <PlayoffCups bracketGroups={groups} matchSections={sections} />
      </MemoryRouter>
    );

    expect(screen.getByText(/Sorteo realizado el/i)).toBeInTheDocument();
  });

  it('shows no draw-date label when drawnAt is null', () => {
    const m = [match('semi', 'A', 'B')];
    const groups = [bracketGroup(null, 'semi', m, null)];
    const sections = [fixtureSection(null, 'semi', m)];

    render(
      <MemoryRouter>
        <PlayoffCups bracketGroups={groups} matchSections={sections} />
      </MemoryRouter>
    );

    expect(screen.queryByText(/Sorteo realizado el/i)).not.toBeInTheDocument();
  });
});
