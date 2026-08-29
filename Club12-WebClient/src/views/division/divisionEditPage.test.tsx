import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import DivisionEditPage from '@/views/division/divisionEditPage';
import { useDivision } from '@/modules/division/hook/division.hook';
import { IDivisionResponse } from '@/modules/division/type/division';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/division/hook/division.hook');
vi.mock('@/modules/core/utils/confirmDialog', () => ({
  notifySuccess: vi.fn(() => Promise.resolve()),
  notifyWarning: vi.fn(() => Promise.resolve()),
}));

const mockedUseDivision = vi.mocked(useDivision);

const DIVISION_ID = 'division-1' as unknown as GUID;

const buildDivision = (): IDivisionResponse =>
  ({
    id: DIVISION_ID,
    name: 'Zona A',
    slug: 'zona-a',
    isFinished: false,
    tournamentId: 'tournament-1' as unknown as GUID,
    isCrossDivisionCup: false,
    positions: [],
  }) as IDivisionResponse;

const putDivisionById = vi.fn().mockResolvedValue(true);

const setup = () => {
  mockedUseDivision.mockReturnValue({
    division: buildDivision(),
    divisions: null,
    getDivisionsById: vi.fn().mockResolvedValue(buildDivision()),
    putDivisionById,
  } as unknown as ReturnType<typeof useDivision>);
};

const renderPage = () =>
  render(
    <MemoryRouter initialEntries={[`/panel/divisiones/${DIVISION_ID}/editar`]}>
      <Routes>
        <Route
          path="/panel/divisiones/:divisionId/editar"
          element={<DivisionEditPage />}
        />
        <Route
          path="/panel/divisiones/:divisionId"
          element={<div>detalle-division</div>}
        />
      </Routes>
    </MemoryRouter>
  );

afterEach(() => {
  vi.clearAllMocks();
});

describe('DivisionEditPage — admin can manage a division (QA wave 1)', () => {
  it('prefills the form and saves name + finished state', async () => {
    setup();
    renderPage();

    const nameField = (await screen.findByLabelText(
      /Nombre/i
    )) as HTMLInputElement;
    expect(nameField.value).toBe('Zona A');

    await userEvent.clear(nameField);
    await userEvent.type(nameField, 'Zona B');
    await userEvent.click(screen.getByRole('button', { name: 'Guardar' }));

    await waitFor(() =>
      expect(putDivisionById).toHaveBeenCalledWith(DIVISION_ID, {
        name: 'Zona B',
        isFinished: false,
      })
    );
    await waitFor(() =>
      expect(screen.getByText('detalle-division')).toBeInTheDocument()
    );
  });
});
