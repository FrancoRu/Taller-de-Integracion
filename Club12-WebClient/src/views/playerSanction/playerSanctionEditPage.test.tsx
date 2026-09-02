import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import PlayerSanctionEditPage from '@/views/playerSanction/playerSanctionEditPage';
import { usePlayerSanction } from '@/modules/playerSanction/hook/playerSanction.hook';
import { IPlayerSanctionResponse } from '@/modules/playerSanction/type/playerSanction.d';

vi.mock('@/modules/playerSanction/hook/playerSanction.hook');
vi.mock('@/modules/core/utils/confirmDialog', () => ({
  notifySuccess: vi.fn(() => Promise.resolve()),
  notifyWarning: vi.fn(() => Promise.resolve()),
}));

const mockedUsePlayerSanction = vi.mocked(usePlayerSanction);

const SANCTION_SLUG = 'sancion-jugador-x-2026';

const buildSanction = (): IPlayerSanctionResponse =>
  ({
    id: 'sanction-guid-1',
    slug: SANCTION_SLUG,
    duration: 2,
    description: 'Conducta antideportiva',
  }) as unknown as IPlayerSanctionResponse;

const renderPage = () =>
  render(
    <MemoryRouter initialEntries={[`/panel/sanciones/editar/${SANCTION_SLUG}`]}>
      <Routes>
        <Route
          path="/panel/sanciones/editar/:playerSanctionId"
          element={<PlayerSanctionEditPage />}
        />
      </Routes>
    </MemoryRouter>
  );

afterEach(() => {
  vi.clearAllMocks();
});

describe('PlayerSanctionEditPage', () => {
  it('renders the form for a sanction reached by slug — not "Sanción no encontrada"', async () => {
    // The detail page's "Editar sanción" button navigates by slug
    // (playerSanction.slug ?? playerSanction.id), never by the raw GUID —
    // the fetched record's own `.id` is still a GUID, so a lookup that
    // compares only `.id` against the route param never matches.
    mockedUsePlayerSanction.mockReturnValue({
      playerSanction: buildSanction(),
      playerSanctions: null,
      getPlayerSanctionById: vi.fn().mockResolvedValue(buildSanction()),
      putPlayerSanctionById: vi.fn().mockResolvedValue(buildSanction()),
    } as unknown as ReturnType<typeof usePlayerSanction>);

    renderPage();

    expect(await screen.findByLabelText(/Duración/i)).toBeInTheDocument();
    expect(screen.queryByText('Sanción no encontrada')).not.toBeInTheDocument();
  });
});
