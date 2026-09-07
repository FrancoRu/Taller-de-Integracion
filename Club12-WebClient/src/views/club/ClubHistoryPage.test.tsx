import { render, screen, waitFor, within } from '@testing-library/react';
import {
  MemoryRouter,
  Route,
  Routes,
  useLocation,
} from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { Mock } from 'vitest';
import userEvent from '@testing-library/user-event';
import ClubHistoryPage from '@/views/club/ClubHistoryPage';
import { useClub } from '@/modules/club/hook/club.hook';
import type {
  IClubContextProps,
  IClubHistoryResponse,
  IClubSummaryResponse,
} from '@/modules/club/type/club.d';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/club/hook/club.hook');
vi.mock('@/modules/core/utils/confirmDialog', () => ({
  confirmAction: vi.fn(() => Promise.resolve(true)),
}));

const mockedUseClub = vi.mocked(useClub);

const CLUB_ID = '11111111-1111-1111-1111-111111111111' as GUID;

const PARENT_SUMMARY: IClubSummaryResponse = {
  id: '22222222-2222-2222-2222-222222222222' as GUID,
  name: 'Echagüe',
  slug: 'echague',
  logoUrl: null,
};

const CHILD_SUMMARY: IClubSummaryResponse = {
  id: '33333333-3333-3333-3333-333333333333' as GUID,
  name: 'Echagüe B',
  slug: 'echague-b',
  logoUrl: null,
};

const CLUB: IClubHistoryResponse = {
  id: CLUB_ID,
  name: 'Colón',
  slug: 'colon',
  logoUrl: null,
  parentClub: null,
  childClubs: [],
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
let getAllClubs: Mock<IClubContextProps['getAllClubs']>;
let linkClubParent: Mock<IClubContextProps['linkClubParent']>;
let unlinkClubParent: Mock<IClubContextProps['unlinkClubParent']>;
let renameClub: Mock<IClubContextProps['renameClub']>;

const setupHook = (
  club: IClubHistoryResponse | null = CLUB,
  allClubs: IClubSummaryResponse[] = []
) => {
  getClubHistory = vi.fn<IClubContextProps['getClubHistory']>();
  getClubHistory.mockResolvedValue(club ?? undefined);
  getAllClubs = vi.fn<IClubContextProps['getAllClubs']>();
  getAllClubs.mockResolvedValue(allClubs);
  linkClubParent = vi.fn<IClubContextProps['linkClubParent']>();
  linkClubParent.mockResolvedValue(club ?? undefined);
  unlinkClubParent = vi.fn<IClubContextProps['unlinkClubParent']>();
  unlinkClubParent.mockResolvedValue(club ?? undefined);
  renameClub = vi.fn<IClubContextProps['renameClub']>();
  renameClub.mockResolvedValue(club ?? undefined);

  mockedUseClub.mockReturnValue({
    club,
    getClubHistory,
    copyRoster: vi.fn(),
    allClubs,
    getAllClubs,
    linkClubParent,
    unlinkClubParent,
    renameClub,
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

// For navigation tests: the destination club's slug is a static route that
// renders only the location probe. Reusing the dynamic :idOrSlug route for
// the destination would re-mount ClubHistoryPage with the SAME mocked
// (unchanged) club, whose own URL-canonicalization effect would immediately
// navigate back since idOrSlug no longer matches club.slug.
const renderPageWithNavigationTargets = (entry = '/panel/clubes/colon') =>
  render(
    <MemoryRouter initialEntries={[entry]}>
      <Routes>
        <Route path={`/panel/clubes/${PARENT_SUMMARY.slug}`} element={<LocationProbe />} />
        <Route path={`/panel/clubes/${CHILD_SUMMARY.slug}`} element={<LocationProbe />} />
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

  describe('parent institution linking', () => {
    it('shows the parent club and navigates to it when clicked', async () => {
      setupHook({ ...CLUB, parentClub: PARENT_SUMMARY });
      renderPageWithNavigationTargets();

      const parentChip = await screen.findByText('Escuadra de Echagüe');
      await userEvent.click(parentChip);

      await waitFor(() =>
        expect(screen.getByTestId('location')).toHaveTextContent(
          '/panel/clubes/echague'
        )
      );
    });

    it('unlinks the parent after confirming', async () => {
      setupHook({ ...CLUB, parentClub: PARENT_SUMMARY });
      renderPage();

      const unlinkButton = await screen.findByRole('button', { name: 'Desvincular' });
      await userEvent.click(unlinkButton);

      await waitFor(() => expect(unlinkClubParent).toHaveBeenCalledWith(CLUB_ID));
    });

    it('lists child squads and navigates to one when clicked', async () => {
      setupHook({ ...CLUB, childClubs: [CHILD_SUMMARY] });
      renderPageWithNavigationTargets();

      const childChip = await screen.findByText('Echagüe B');
      await userEvent.click(childChip);

      await waitFor(() =>
        expect(screen.getByTestId('location')).toHaveTextContent(
          '/panel/clubes/echague-b'
        )
      );
    });

    it('offers a parent picker for a club with no parent and no squads, and links the chosen one', async () => {
      setupHook(CLUB, [PARENT_SUMMARY]);
      renderPage();

      await screen.findByRole('heading', { name: 'Colón' });
      await waitFor(() => expect(getAllClubs).toHaveBeenCalled());

      await userEvent.click(screen.getByLabelText('Vincular con club matriz'));
      await userEvent.click(await screen.findByRole('option', { name: 'Echagüe' }));
      await userEvent.click(screen.getByRole('button', { name: 'Vincular' }));

      await waitFor(() =>
        expect(linkClubParent).toHaveBeenCalledWith(CLUB_ID, PARENT_SUMMARY.id)
      );
    });

    it('does not offer the parent picker once the club already has its own squads', async () => {
      setupHook({ ...CLUB, childClubs: [CHILD_SUMMARY] }, [PARENT_SUMMARY]);
      renderPage();

      await screen.findByRole('heading', { name: 'Colón' });
      expect(screen.queryByLabelText('Vincular con club matriz')).not.toBeInTheDocument();
      expect(getAllClubs).not.toHaveBeenCalled();
    });

    it('does not offer the parent picker once the club already has a parent', async () => {
      setupHook({ ...CLUB, parentClub: PARENT_SUMMARY }, [PARENT_SUMMARY]);
      renderPage();

      await screen.findByRole('heading', { name: 'Colón' });
      expect(screen.queryByLabelText('Vincular con club matriz')).not.toBeInTheDocument();
      expect(getAllClubs).not.toHaveBeenCalled();
    });
  });

  describe('renaming the club', () => {
    it('renames the club after editing and saving the name', async () => {
      setupHook();
      renderPage();

      await screen.findByRole('heading', { name: 'Colón' });
      await userEvent.click(screen.getByLabelText('Editar nombre del club'));

      const input = screen.getByDisplayValue('Colón');
      await userEvent.clear(input);
      await userEvent.type(input, 'Echagüe');
      await userEvent.click(screen.getByRole('button', { name: 'Guardar' }));

      await waitFor(() =>
        expect(renameClub).toHaveBeenCalledWith(CLUB_ID, 'Echagüe')
      );
    });

    it('discards the draft when cancelled, without renaming', async () => {
      setupHook();
      renderPage();

      await screen.findByRole('heading', { name: 'Colón' });
      await userEvent.click(screen.getByLabelText('Editar nombre del club'));
      await userEvent.type(screen.getByDisplayValue('Colón'), ' cambiado');
      await userEvent.click(screen.getByRole('button', { name: 'Cancelar' }));

      expect(screen.getByRole('heading', { name: 'Colón' })).toBeInTheDocument();
      expect(renameClub).not.toHaveBeenCalled();
    });
  });
});
