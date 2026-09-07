import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { IStageResponse, StageType } from '@/modules/stage/type/stage';
import DivisionFixture from '@/views/division/DivisionFixture';

const getStagesByFilters = vi.fn().mockResolvedValue({ data: { items: [] } });
const getMatchByFilter = vi.fn().mockResolvedValue({ data: { items: [] } });

vi.mock('@/modules/stage/service/stage.service', () => ({
  stageService: { getStagesByFilters: (...args: unknown[]) => getStagesByFilters(...args) },
}));
vi.mock('@/modules/match/service/match.service', () => ({
  matchService: { getMatchByFilter: (...args: unknown[]) => getMatchByFilter(...args) },
}));

const guid = (value: string) => value as GUID;

const groupStage: IStageResponse = {
  id: guid('stage-group'),
  name: 'Zona A - Fase de Grupos',
  slug: 'zona-a-fase-de-grupos',
  stageType: StageType.Group,
  isActive: true,
  isElimination: false,
  startDate: '2026-01-01T00:00:00Z',
  endDate: '2026-02-01T00:00:00Z',
  divisionId: guid('division-1'),
  order: 0,
  bestOf: 1,
  roundRobinLegs: 1,
};

const renderFixture = () =>
  render(
    <MemoryRouter>
      <DivisionFixture divisionId={guid('division-1')} divisionName="Zona A" />
    </MemoryRouter>
  );

describe('DivisionFixture — empty state', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('explains where the matches are, instead of a generic empty state, when the division is groupless', async () => {
    // A playoffs-only division has no Group-type stage at all — this tab is
    // empty by design, not because nothing was ever scheduled.
    getStagesByFilters.mockResolvedValue({ data: { items: [] } });
    getMatchByFilter.mockResolvedValue({ data: { items: [] } });

    renderFixture();

    expect(
      await screen.findByText(/sus partidos están en la pestaña Playoff/i)
    ).toBeInTheDocument();
    expect(screen.queryByText(/^No hay partidos registrados/)).not.toBeInTheDocument();
  });

  it('shows the generic empty state when the division has a group stage but no scheduled matches', async () => {
    getStagesByFilters.mockResolvedValue({ data: { items: [groupStage] } });
    getMatchByFilter.mockResolvedValue({ data: { items: [] } });

    renderFixture();

    await waitFor(() => expect(getMatchByFilter).toHaveBeenCalled());

    expect(
      await screen.findByText('No hay partidos registrados en esta división.')
    ).toBeInTheDocument();
    expect(screen.queryByText(/pestaña Playoff/i)).not.toBeInTheDocument();
  });
});
