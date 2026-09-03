import { render, screen, waitFor, within } from '@testing-library/react';
import {
  MemoryRouter,
  Route,
  Routes,
  useLocation,
} from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { Mock } from 'vitest';
import ClubHistoryPage from '@/views/club/ClubHistoryPage';
import { useClub } from '@/modules/club/hook/club.hook';
import type {
  IClubContextProps,
  IClubHistoryResponse,
} from '@/modules/club/type/club.d';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/club/hook/club.hook');

const mockedUseClub = vi.mocked(useClub);

const CLUB_ID = '11111111-1111-1111-1111-111111111111' as GUID;

const CLUB: IClubHistoryResponse = {
  id: CLUB_ID,
  name: 'Colón',
  slug: 'colon',
  logoUrl: null,
  teams: [
    {
      teamId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' as GUID,
      name: 'Colón 2026',
      slug: 'colon-2026',
      threeLetterCode: 'COL',
      seasons: [
        {
          tournamentId: 'c1c1c1c1-c1c1-c1c1-c1c1-c1c1c1c1c1c1' as GUID,
          tournamentName: 'Apertura 2026',
          startDate: '2026-03-01T00:00:00Z',
        },
      ],
    },
    {
      teamId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb' as GUID,
      name: 'Colón 2027',
      slug: 'colon-2027',
      threeLetterCode: 'COL',
      seasons: [
        {
          tournamentId: 'c2c2c2c2-c2c2-c2c2-c2c2-c2c2c2c2c2c2' as GUID,
          tournamentName: 'Apertura 2027',
          startDate: '2027-03-01T00:00:00Z',
        },
      ],
    },
  ],
};

const LocationProbe = () => {
  const location = useLocation();
  return <div data-testid="location">{location.pathname}</div>;
};

let getClubHistory: Mock<IClubContextProps['getClubHistory']>;

const setupHook = (club: IClubHistoryResponse | null = CLUB) => {
  getClubHistory = vi.fn<IClubContextProps['getClubHistory']>();
  getClubHistory.mockResolvedValue(club ?? undefined);

  mockedUseClub.mockReturnValue({
    club,
    getClubHistory,
    copyRoster: vi.fn(),
  } satisfies IClubContextProps);
};

const renderPage = (entry = '/panel/clubes/colon') =>
  render(
    <MemoryRouter initialEntries={[entry]}>
      <Routes>
        <Route
          path="/panel/clubes/:idOrSlug"
          element={
            <>
              <LocationProbe />
              <ClubHistoryPage />
            </>
          }
        />
      </Routes>
    </MemoryRouter>
  );

beforeEach(() => {
  setupHook();
});

afterEach(() => {
  vi.clearAllMocks();
});

describe('ClubHistoryPage', () => {
  it('fetches the club history for the route param', async () => {
    renderPage();
    await waitFor(() => expect(getClubHistory).toHaveBeenCalledWith('colon'));
  });

  it('renders the club header and one row per season', async () => {
    renderPage();

    expect(
      await screen.findByRole('heading', { name: 'Colón' })
    ).toBeInTheDocument();

    const rowGroups = screen.getAllByRole('rowgroup');
    // rowGroups[1] is the <tbody>.
    const bodyRows = within(rowGroups[1]).getAllByRole('row');
    expect(bodyRows).toHaveLength(2);

    expect(screen.getByText('Colón 2026')).toBeInTheDocument();
    expect(screen.getByText('Apertura 2026')).toBeInTheDocument();
    expect(screen.getByText('Colón 2027')).toBeInTheDocument();
    expect(screen.getByText('Apertura 2027')).toBeInTheDocument();
  });

  it('orders the rows by season start date, newest first', async () => {
    renderPage();

    const rowGroups = await screen.findAllByRole('rowgroup');
    const bodyRows = within(rowGroups[1]).getAllByRole('row');

    expect(within(bodyRows[0]).getByText('Apertura 2027')).toBeInTheDocument();
    expect(within(bodyRows[1]).getByText('Apertura 2026')).toBeInTheDocument();
  });

  it('replaces a GUID URL with the club slug once loaded', async () => {
    renderPage(`/panel/clubes/${CLUB_ID}`);

    await waitFor(() =>
      expect(screen.getByTestId('location')).toHaveTextContent(
        '/panel/clubes/colon'
      )
    );
  });

  it('leaves the URL untouched when it already is the slug', async () => {
    renderPage('/panel/clubes/colon');

    await screen.findByRole('heading', { name: 'Colón' });
    expect(screen.getByTestId('location')).toHaveTextContent(
      '/panel/clubes/colon'
    );
  });

  it('shows a not-found card when no club is loaded', async () => {
    setupHook(null);
    renderPage();

    expect(
      await screen.findByRole('heading', { name: 'Club no encontrado' })
    ).toBeInTheDocument();
  });
});
