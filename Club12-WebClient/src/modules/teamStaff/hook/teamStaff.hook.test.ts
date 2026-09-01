import { act, renderHook, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { useTeamStaff } from '@/modules/teamStaff/hook/teamStaff.hook';
import { teamStaffService } from '@/modules/teamStaff/service/teamStaff.service';
import { ITeamStaffResponse } from '@/modules/teamStaff/type/teamStaff';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/teamStaff/service/teamStaff.service');

const mocked = vi.mocked(teamStaffService);
const TEAM_ID = 'team-1' as unknown as GUID;
const TOURNAMENT_ID = 'tournament-1' as unknown as GUID;

const buildStaff = (
  overrides: Partial<ITeamStaffResponse>
): ITeamStaffResponse => ({
  id: 'staff-1' as unknown as GUID,
  teamId: TEAM_ID,
  teamName: 'Aguará',
  tournamentId: TOURNAMENT_ID,
  fullName: 'Juan Pérez',
  role: 'Coach',
  dateCreated: '2026-01-01T00:00:00Z',
  ...overrides,
});

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const asAxios = <T,>(data: T) => ({ data }) as any;

describe('useTeamStaff', () => {
  afterEach(() => {
    vi.clearAllMocks();
  });

  it('loads the team staff on mount', async () => {
    mocked.getTeamStaffByTeamId.mockResolvedValue(asAxios([buildStaff({})]));

    const { result } = renderHook(() => useTeamStaff(TEAM_ID, TOURNAMENT_ID));

    await waitFor(() => expect(result.current.staff).toHaveLength(1));
    expect(mocked.getTeamStaffByTeamId).toHaveBeenCalledWith(
      TEAM_ID,
      TOURNAMENT_ID
    );
  });

  it('stays idle without a team id', async () => {
    const { result } = renderHook(() =>
      useTeamStaff(undefined, TOURNAMENT_ID)
    );

    await act(async () => {
      await result.current.refresh();
    });

    expect(mocked.getTeamStaffByTeamId).not.toHaveBeenCalled();
    expect(result.current.staff).toEqual([]);
  });

  it('stays idle without a tournament id', async () => {
    const { result } = renderHook(() => useTeamStaff(TEAM_ID, undefined));

    await act(async () => {
      await result.current.refresh();
    });

    expect(mocked.getTeamStaffByTeamId).not.toHaveBeenCalled();
    expect(result.current.staff).toEqual([]);
  });

  it('creates a staff member then refreshes the list', async () => {
    mocked.getTeamStaffByTeamId
      .mockResolvedValueOnce(asAxios([]))
      .mockResolvedValueOnce(asAxios([buildStaff({})]));
    mocked.addTeamStaff.mockResolvedValue(asAxios(buildStaff({})));

    const { result } = renderHook(() => useTeamStaff(TEAM_ID, TOURNAMENT_ID));
    await waitFor(() =>
      expect(mocked.getTeamStaffByTeamId).toHaveBeenCalledTimes(1)
    );

    await act(async () => {
      await result.current.create({
        fullName: 'Juan Pérez',
        role: 'Coach',
        tournamentId: TOURNAMENT_ID,
      });
    });

    expect(mocked.addTeamStaff).toHaveBeenCalledWith(TEAM_ID, {
      fullName: 'Juan Pérez',
      role: 'Coach',
      tournamentId: TOURNAMENT_ID,
    });
    await waitFor(() => expect(result.current.staff).toHaveLength(1));
  });

  it('removes a staff member then refreshes the list', async () => {
    mocked.getTeamStaffByTeamId
      .mockResolvedValueOnce(asAxios([buildStaff({})]))
      .mockResolvedValueOnce(asAxios([]));
    mocked.deleteTeamStaff.mockResolvedValue(asAxios(undefined));

    const { result } = renderHook(() => useTeamStaff(TEAM_ID, TOURNAMENT_ID));
    await waitFor(() => expect(result.current.staff).toHaveLength(1));

    await act(async () => {
      await result.current.remove('staff-1' as unknown as GUID);
    });

    expect(mocked.deleteTeamStaff).toHaveBeenCalledWith('staff-1');
    await waitFor(() => expect(result.current.staff).toHaveLength(0));
  });
});
