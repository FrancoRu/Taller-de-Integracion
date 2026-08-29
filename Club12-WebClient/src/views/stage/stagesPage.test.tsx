import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import StagesPage from '@/views/stage/stagesPage';
import { useStage } from '@/modules/stage/hook/stage.hook';
import { useDivision } from '@/modules/division/hook/division.hook';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import {
  IStageContextProps,
  IStageResponse,
  StageType,
} from '@/modules/stage/type/stage';
import { TABLE_ROWS_PER_PAGE } from '@/modules/core/constants/pagination';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/stage/hook/stage.hook');
vi.mock('@/modules/division/hook/division.hook');
vi.mock('@/modules/tournament/hook/tournament.hook');
vi.mock('@/modules/core/utils/confirmDialog', () => ({
  confirmDelete: vi.fn().mockResolvedValue(true),
  notifySuccess: vi.fn().mockResolvedValue(undefined),
}));

const mockedUseStage = vi.mocked(useStage);
const mockedUseDivision = vi.mocked(useDivision);
const mockedUseTournament = vi.mocked(useTournament);

const DIVISION_ID = 'division-1-aaaa-bbbb-cccc' as unknown as GUID;

const buildStage = (): IStageResponse => ({
  id: 'stage-1-aaaa-bbbb-cccc' as unknown as GUID,
  name: 'Semifinal',
  slug: 'semifinal',
  stageType: StageType.SemiFinal,
  isActive: true,
  isElimination: true,
  startDate: '2026-01-01T00:00:00.000Z',
  endDate: '2026-01-08T00:00:00.000Z',
  divisionId: DIVISION_ID,
  order: 0,
  bestOf: 1,
  roundRobinLegs: 1,
});

const setupHooks = () => {
  const getStagesByFilters = vi
    .fn<IStageContextProps['getStagesByFilters']>()
    .mockResolvedValue({
      items: [buildStage()],
      page: 1,
      pageSize: TABLE_ROWS_PER_PAGE,
      totalCount: 1,
    });

  mockedUseStage.mockReturnValue({
    stages: [buildStage()],
    getStagesByFilters,
    deleteStagesById: vi.fn().mockResolvedValue(undefined),
    generateStagesAutomatically: vi.fn().mockResolvedValue(undefined),
  } as unknown as IStageContextProps);

  mockedUseDivision.mockReturnValue({
    divisions: [],
    getDivisionsByFilters: vi.fn(),
  } as unknown as ReturnType<typeof useDivision>);

  mockedUseTournament.mockReturnValue({
    tournaments: [],
    getAllTournamentsByFilter: vi.fn(),
  } as unknown as ReturnType<typeof useTournament>);
};

class ResizeObserverStub {
  observe() {}
  unobserve() {}
  disconnect() {}
}

/**
 * MUI DataGrid virtualizes rows/columns based on measured container size.
 * jsdom reports zero layout dimensions, which hides columns like the actions
 * column entirely. Stub non-zero dimensions so all columns render.
 */
const stubLayoutDimensions = () => {
  Object.defineProperties(window.HTMLElement.prototype, {
    offsetWidth: { configurable: true, get: () => 1000 },
    offsetHeight: { configurable: true, get: () => 1000 },
    clientWidth: { configurable: true, get: () => 1000 },
    clientHeight: { configurable: true, get: () => 1000 },
  });
  window.HTMLElement.prototype.getBoundingClientRect = () =>
    ({
      width: 1000,
      height: 1000,
      top: 0,
      left: 0,
      right: 1000,
      bottom: 1000,
      x: 0,
      y: 0,
      toJSON() {},
    }) as DOMRect;
};

const renderStagesPage = (stageStructureLocked: boolean) =>
  render(
    <MemoryRouter>
      <StagesPage
        divisionId={DIVISION_ID}
        stageStructureLocked={stageStructureLocked}
        wrapInCard={false}
      />
    </MemoryRouter>
  );

beforeEach(() => {
  setupHooks();
  if (!window.ResizeObserver) {
    window.ResizeObserver =
      ResizeObserverStub as unknown as typeof ResizeObserver;
  }
  stubLayoutDimensions();
});

afterEach(() => {
  vi.clearAllMocks();
});

describe('StagesPage — phase-structure lock (started tournament)', () => {
  it('keeps the "Nueva Fase" button and the delete action available when the structure is editable', async () => {
    renderStagesPage(false);

    const createButton = screen.getByRole('button', { name: /nueva fase/i });
    expect(createButton).toBeEnabled();

    // The actions column renders: "Ver partidos" plus "Eliminar".
    await screen.findByTestId('VisibilityIcon');
    expect(screen.getByTestId('DeleteIcon')).toBeInTheDocument();
  });

  it('disables the "Nueva Fase" button and hides the delete action when the tournament has started', async () => {
    const user = userEvent.setup();
    renderStagesPage(true);

    const createButton = screen.getByRole('button', { name: /nueva fase/i });
    expect(createButton).toBeDisabled();

    // The explanatory tooltip is anchored on the wrapper span (a disabled
    // button swallows hover events on its own).
    await user.hover(createButton.parentElement as HTMLElement);
    await waitFor(() =>
      expect(screen.getByText('El torneo ya arrancó')).toBeInTheDocument()
    );

    // The actions column still renders "Ver partidos" but no longer the
    // "Eliminar" action.
    await screen.findByTestId('VisibilityIcon');
    expect(screen.queryByTestId('DeleteIcon')).not.toBeInTheDocument();
  });
});
