import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import DivisionCreatePage from '@/views/division/divisionCreatePage';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';
import type { GUID } from '@/modules/core/types/types';
import type { ITournamentContextProps } from '@/modules/tournament/type/tournament.d';

vi.mock('@/modules/tournament/hook/tournament.hook');

const mockedUseTournament = vi.mocked(useTournament);

const buildTournament = (
  id: string,
  name: string,
  status: TournamentStatus
) => ({
  id: id as unknown as GUID,
  name,
  slug: id,
  description: '',
  divisions: [],
  teamRegistrationDeadline: new Date('2026-01-01'),
  startDate: new Date('2026-02-01'),
  status,
  category: TournamentCategory.Masculine,
  seasonId: null,
  seasonName: null,
});

const renderPage = () =>
  render(
    <MemoryRouter initialEntries={['/panel/divisiones/crear']}>
      <Routes>
        <Route path="/panel/divisiones/crear" element={<DivisionCreatePage />} />
      </Routes>
    </MemoryRouter>
  );

describe('DivisionCreatePage — tournament picker only offers open-registration tournaments', () => {
  it('excludes Ongoing (and other frozen-status) tournaments from the "Torneo" select', async () => {
    mockedUseTournament.mockReturnValue({
      tournament: null,
      tournaments: [
        buildTournament('t-open', 'Apertura (abierta)', TournamentStatus.OpenForRegistration),
        buildTournament('t-ongoing', 'Clausura (en curso)', TournamentStatus.Ongoing),
      ],
      addTournament: vi.fn(),
      createFullTournament: vi.fn(),
      addFullDivision: vi.fn(),
      getAllTournamentsByFilter: vi.fn(),
      getTournamentById: vi.fn(),
      putTournamentById: vi.fn(),
      deleteTournamentById: vi.fn(),
      registerTeamsByTournamentId: vi.fn(),
      enrollTeam: vi.fn(),
      unenrollTeam: vi.fn(),
      getCompletability: vi.fn(),
    } as ITournamentContextProps);

    renderPage();

    const select = await screen.findByRole('combobox', { name: /Torneo/ });
    await userEvent.click(select);

    const listbox = await screen.findByRole('listbox');
    expect(
      within(listbox).getByText('Apertura (abierta)')
    ).toBeInTheDocument();
    expect(
      within(listbox).queryByText('Clausura (en curso)')
    ).not.toBeInTheDocument();
  });
});
