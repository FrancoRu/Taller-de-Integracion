import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import TournamentWizardPage from './TournamentWizardPage';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { useSeason } from '@/modules/season/hook/season.hook';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';
import { createEmptyZone, createInitialWizardState, WizardState } from './types';

vi.mock('@/modules/tournament/hook/tournament.hook');
vi.mock('@/modules/season/hook/season.hook');

const mockedUseTournament = vi.mocked(useTournament);
const mockedUseSeason = vi.mocked(useSeason);

const WIZARD_PATH = '/panel/torneos/asistente';

const renderWizard = (state?: unknown) =>
  render(
    <MemoryRouter initialEntries={[{ pathname: WIZARD_PATH, state }]}>
      <Routes>
        <Route path={WIZARD_PATH} element={<TournamentWizardPage />} />
      </Routes>
    </MemoryRouter>
  );

describe('TournamentWizardPage — clone prefill (HU-cloning)', () => {
  beforeEach(() => {
    mockedUseTournament.mockReturnValue({
      createFullTournament: vi.fn(),
    } as unknown as ReturnType<typeof useTournament>);
    mockedUseSeason.mockReturnValue({
      seasons: [],
      getSeasonsByFiltered: vi.fn(),
    } as unknown as ReturnType<typeof useSeason>);
  });

  it('keeps the pre-existing { seasonId }-only launch working unchanged', () => {
    renderWizard({ seasonId: 'season-123' });

    expect(screen.getByLabelText(/^Nombre/)).toHaveValue('');
    // No review banner when the wizard was not launched from a clone.
    expect(screen.queryByText(/necesitan revisión/i)).not.toBeInTheDocument();
  });

  it('pre-fills the wizard state from clonePrefill', () => {
    const zone = { ...createEmptyZone(), name: 'Zona A' };
    const clonePrefill: WizardState = {
      ...createInitialWizardState(),
      tournament: {
        ...createInitialWizardState().tournament,
        name: 'Apertura 2026 (copia)',
        category: TournamentCategory.Feminine,
      },
      zones: [zone],
    };

    renderWizard({ clonePrefill, cloneReview: [] });

    expect(screen.getByLabelText(/^Nombre/)).toHaveValue('Apertura 2026 (copia)');
  });

  it('renders the cloneReview notices as a persistent banner when non-empty', () => {
    const clonePrefill = createInitialWizardState();
    const cloneReview = ['La copa "Copa Oro" de la zona "Zona A" no tiene un mapeo asociado.'];

    renderWizard({ clonePrefill, cloneReview });

    expect(screen.getByText(/necesitan revisión/i)).toBeInTheDocument();
    expect(screen.getByText(cloneReview[0])).toBeInTheDocument();
  });

  it('does not render a banner when cloneReview is empty', () => {
    renderWizard({ clonePrefill: createInitialWizardState(), cloneReview: [] });

    expect(screen.queryByText(/necesitan revisión/i)).not.toBeInTheDocument();
  });
});
