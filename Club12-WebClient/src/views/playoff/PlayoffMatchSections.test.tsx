import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { MatchType } from '@/modules/core/enum/match/matchType';
import { IMatchResponse } from '@/modules/match/type/match';
import { ITeamMatchResponse } from '@/modules/team/type/team';
import { IStageResponse, StageType } from '@/modules/stage/type/stage';
import { buildDivisionFixtureSections } from '@/modules/match/utils/divisionFixtureSections';
import PlayoffMatchSections from '@/views/playoff/PlayoffMatchSections';

const guid = (value: string) => value as GUID;

const team = (name: string): ITeamMatchResponse => ({
  id: guid(`team-${name}`),
  name,
  logoUrl: '',
  score: 0,
  players: [],
  scorers: [],
});

let sequence = 0;

const match = (stageId: string, overrides: Partial<IMatchResponse> = {}): IMatchResponse => ({
  id: guid(`match-${(sequence += 1)}`),
  matchDate: '2026-04-28T20:00:00Z',
  round: null,
  matchType: MatchType.Playoff,
  slug: `match-${sequence}`,
  homeTeam: team('A'),
  visitorTeam: team('B'),
  isFinished: false,
  winningTeamId: null,
  venue: null,
  stageId: guid(stageId),
  winningTeamName: null,
  status: null,
  ...overrides,
});

const stage = (
  overrides: Partial<Omit<IStageResponse, 'id'>> & { id: string; name: string }
): IStageResponse => ({
  slug: `stage-${overrides.id}`,
  description: null,
  stageType: StageType.SemiFinal,
  isActive: true,
  isElimination: true,
  startDate: '2026-04-01T00:00:00Z',
  endDate: '2026-05-01T00:00:00Z',
  divisionId: guid('division-1'),
  order: 0,
  bracketName: null,
  bestOf: 1,
  roundRobinLegs: 1,
  ...overrides,
  id: guid(overrides.id),
});

const renderSections = (sections: ReturnType<typeof buildDivisionFixtureSections>) =>
  render(
    <MemoryRouter>
      <PlayoffMatchSections sections={sections} />
    </MemoryRouter>
  );

describe('PlayoffMatchSections', () => {
  it('wraps each cup in its own bordered card with a trophy-accented title', () => {
    const stages = [
      stage({ id: 'oro-semi', name: 'Semifinales Copa Oro', stageType: StageType.SemiFinal, bracketName: 'Copa Oro', order: 1 }),
      stage({ id: 'plata-semi', name: 'Semifinales Copa Plata', stageType: StageType.SemiFinal, bracketName: 'Copa Plata', order: 2 }),
    ];
    const matches = [match('oro-semi'), match('plata-semi')];
    const sections = buildDivisionFixtureSections(stages, matches, 'Zona A');

    const { container } = renderSections(sections);

    const oroTitle = screen.getByText('Copa Oro');
    const plataTitle = screen.getByText('Copa Plata');
    // Each cup's title sits inside its own bordered card, with a trophy icon
    // right next to it — not just a bare heading floating in the list.
    expect(oroTitle.closest('.MuiPaper-root')).not.toBeNull();
    expect(plataTitle.closest('.MuiPaper-root')).not.toBeNull();
    expect(oroTitle.closest('.MuiPaper-root')).not.toBe(plataTitle.closest('.MuiPaper-root'));
    expect(container.querySelectorAll('[data-testid="EmojiEventsIcon"]')).toHaveLength(2);
  });

  it('renders with no extra card wrapper when the division has a single, unnamed bracket', () => {
    const stages = [
      stage({ id: 'semi', name: 'Zona A - Semifinal', stageType: StageType.SemiFinal, order: 1 }),
    ];
    const matches = [match('semi')];
    const sections = buildDivisionFixtureSections(stages, matches, 'Zona A');

    const { container } = renderSections(sections);

    expect(screen.getByText('Semifinal')).toBeInTheDocument();
    // No cup title/trophy icon — nothing to separate a single bracket from.
    expect(container.querySelectorAll('[data-testid="EmojiEventsIcon"]')).toHaveLength(0);
  });
});
