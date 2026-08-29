import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import StagePage from '@/views/stage/stagePage';
import { useStage } from '@/modules/stage/hook/stage.hook';
import { IStageResponse, StageType } from '@/modules/stage/type/stage';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/stage/hook/stage.hook');
// The "Partidos" tab pulls in the whole match/tournament/division data subtree
// it does not need for these navigation/affordance tests; stub it so only the
// stage-page wiring is exercised.
vi.mock('@/views/match/matchesPage', () => ({
  default: () => <div>partidos-panel</div>,
}));

const mockedUseStage = vi.mocked(useStage);

const STAGE_ID = 'stage-1' as unknown as GUID;

const buildStage = (): IStageResponse =>
  ({
    id: STAGE_ID,
    name: 'Fase de grupos',
    slug: 'fase-de-grupos',
    stageType: StageType.Group,
    isActive: true,
    isElimination: false,
    startDate: '2026-01-01',
    endDate: '2026-02-01',
    divisionId: 'division-1' as unknown as GUID,
    order: 1,
    bestOf: 1,
    roundRobinLegs: 1,
  }) as IStageResponse;

const setup = () => {
  mockedUseStage.mockReturnValue({
    stage: buildStage(),
    stages: null,
    getStageById: vi.fn().mockResolvedValue(buildStage()),
  } as unknown as ReturnType<typeof useStage>);
};

const renderPage = () =>
  render(
    <MemoryRouter initialEntries={[`/panel/fases/${STAGE_ID}`]}>
      <Routes>
        <Route path="/panel/fases/:stageId" element={<StagePage />} />
        <Route path="/panel/fases" element={<div>listado-fases</div>} />
      </Routes>
    </MemoryRouter>
  );

afterEach(() => {
  vi.clearAllMocks();
});

describe('StagePage — stages are not editable (QA wave 1)', () => {
  it('does not render an "Editar" affordance for the stage', async () => {
    setup();
    renderPage();

    await screen.findByRole('tab', { name: 'Detalle' });
    expect(
      screen.queryByRole('button', { name: 'Editar' })
    ).not.toBeInTheDocument();
  });

  it('exposes a "Partidos" tab to view the stage matches', async () => {
    setup();
    renderPage();

    expect(
      await screen.findByRole('tab', { name: 'Partidos' })
    ).toBeInTheDocument();
  });

  it('"Volver" navigates back to the stages list', async () => {
    setup();
    renderPage();

    const volver = await screen.findByRole('button', { name: 'Volver' });
    await userEvent.click(volver);

    expect(screen.getByText('listado-fases')).toBeInTheDocument();
  });
});
