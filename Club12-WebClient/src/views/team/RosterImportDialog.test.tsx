import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { Mock } from 'vitest';
import RosterImportDialog from '@/views/team/RosterImportDialog';
import { useClub } from '@/modules/club/hook/club.hook';
import { notifySuccess, notifyWarning } from '@/modules/core/utils/confirmDialog';
import type {
  IClubContextProps,
  IClubHistoryResponse,
} from '@/modules/club/type/club.d';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/club/hook/club.hook');
vi.mock('@/modules/core/utils/confirmDialog', () => ({
  notifySuccess: vi.fn().mockResolvedValue(undefined),
  notifyWarning: vi.fn().mockResolvedValue(undefined),
}));

const mockedUseClub = vi.mocked(useClub);
const mockedNotifySuccess = vi.mocked(notifySuccess);
const mockedNotifyWarning = vi.mocked(notifyWarning);

const TARGET_TEAM_ID = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' as GUID;
const TARGET_TOURNAMENT_ID = 'c2c2c2c2-c2c2-c2c2-c2c2-c2c2c2c2c2c2' as GUID;
const CLUB_ID = '11111111-1111-1111-1111-111111111111' as GUID;
const SOURCE_TEAM_ID = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb' as GUID;
const SOURCE_TOURNAMENT_ID = 'c1c1c1c1-c1c1-c1c1-c1c1-c1c1c1c1c1c1' as GUID;

const HISTORY: IClubHistoryResponse = {
  id: CLUB_ID,
  name: 'Colón',
  slug: 'colon',
  logoUrl: null,
  teams: [
    {
      teamId: SOURCE_TEAM_ID,
      name: 'Colón 2026',
      slug: 'colon-2026',
      threeLetterCode: 'COL',
      seasons: [
        { tournamentId: SOURCE_TOURNAMENT_ID, tournamentName: 'Apertura 2026' },
      ],
    },
    {
      // The current team + season must be excluded from the source options.
      teamId: TARGET_TEAM_ID,
      name: 'Colón 2027',
      slug: 'colon-2027',
      threeLetterCode: 'COL',
      seasons: [
        { tournamentId: TARGET_TOURNAMENT_ID, tournamentName: 'Apertura 2027' },
      ],
    },
  ],
};

let getClubHistory: Mock<IClubContextProps['getClubHistory']>;
let copyRoster: Mock<IClubContextProps['copyRoster']>;
let onClose: Mock;
let onImported: Mock;

const setupHook = () => {
  getClubHistory = vi.fn<IClubContextProps['getClubHistory']>();
  getClubHistory.mockResolvedValue(HISTORY);
  copyRoster = vi.fn<IClubContextProps['copyRoster']>();
  copyRoster.mockResolvedValue({ copiedCount: 3, skippedCount: 1 });

  mockedUseClub.mockReturnValue({
    club: HISTORY,
    getClubHistory,
    copyRoster,
  } satisfies IClubContextProps);
};

const renderDialog = (
  props: Partial<React.ComponentProps<typeof RosterImportDialog>> = {}
) => {
  onClose = vi.fn();
  onImported = vi.fn();

  return render(
    <RosterImportDialog
      open
      onClose={onClose}
      onImported={onImported}
      targetTeamId={TARGET_TEAM_ID}
      targetTournamentId={TARGET_TOURNAMENT_ID}
      clubId={CLUB_ID}
      {...props}
    />
  );
};

beforeEach(() => {
  setupHook();
});

afterEach(() => {
  vi.clearAllMocks();
});

describe('RosterImportDialog', () => {
  it('warns that the ficha médica is not copied', async () => {
    renderDialog();
    await waitFor(() => expect(getClubHistory).toHaveBeenCalledWith(CLUB_ID));

    expect(
      screen.getByText(/La ficha médica NO se copia/i)
    ).toBeInTheDocument();
  });

  it('lists sibling seasons excluding the current team + season', async () => {
    const user = userEvent.setup();
    renderDialog();
    await waitFor(() => expect(getClubHistory).toHaveBeenCalledWith(CLUB_ID));

    await user.click(screen.getByRole('combobox', { name: /Plantel origen/i }));

    const listbox = await screen.findByRole('listbox');
    expect(
      within(listbox).getByText('Colón 2026 · Apertura 2026')
    ).toBeInTheDocument();
    // The target team's own season must not be selectable as a source.
    expect(
      within(listbox).queryByText('Colón 2027 · Apertura 2027')
    ).not.toBeInTheDocument();
  });

  it('calls copyRoster with the selected source and shows the counts', async () => {
    const user = userEvent.setup();
    renderDialog();
    await waitFor(() => expect(getClubHistory).toHaveBeenCalledWith(CLUB_ID));

    await user.click(screen.getByRole('combobox', { name: /Plantel origen/i }));
    const listbox = await screen.findByRole('listbox');
    await user.click(
      within(listbox).getByText('Colón 2026 · Apertura 2026')
    );

    await user.click(screen.getByRole('button', { name: /Importar/i }));

    await waitFor(() =>
      expect(copyRoster).toHaveBeenCalledWith(TARGET_TEAM_ID, {
        sourceTeamId: SOURCE_TEAM_ID,
        sourceTournamentId: SOURCE_TOURNAMENT_ID,
        targetTournamentId: TARGET_TOURNAMENT_ID,
      })
    );

    await waitFor(() =>
      expect(mockedNotifySuccess).toHaveBeenCalledWith(
        expect.objectContaining({
          title: 'Plantel importado',
          text: expect.stringContaining('3 jugador(es) copiado(s), 1 omitido(s)'),
        })
      )
    );
    expect(onImported).toHaveBeenCalledTimes(1);
  });

  it('disables Importar when the club has no other season to import from', async () => {
    getClubHistory.mockResolvedValueOnce({
      ...HISTORY,
      // Only the target team/season itself — nothing importable.
      teams: [HISTORY.teams[1]],
    });

    renderDialog();
    await waitFor(() => expect(getClubHistory).toHaveBeenCalledWith(CLUB_ID));

    expect(
      await screen.findByText(/todavía no tiene otro equipo/i)
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Importar/i })).toBeDisabled();
  });

  it('does not call copyRoster when no source is selected', async () => {
    const user = userEvent.setup();
    renderDialog();
    await waitFor(() => expect(getClubHistory).toHaveBeenCalledWith(CLUB_ID));

    await user.click(screen.getByRole('button', { name: /Importar/i }));

    await waitFor(() => expect(mockedNotifyWarning).toHaveBeenCalled());
    expect(copyRoster).not.toHaveBeenCalled();
  });
});
