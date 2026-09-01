import { useCallback, useEffect, useState } from 'react';
import { GUID } from '@/modules/core/types/types';
import { teamStaffService } from '@/modules/teamStaff/service/teamStaff.service';
import {
  ICreateTeamStaffRequest,
  ITeamStaffResponse,
} from '@/modules/teamStaff/type/teamStaff';

/**
 * The shape returned by {@link useTeamStaff}.
 */
export interface UseTeamStaff {
  /** The team's technical staff for the given tournament. */
  staff: ITeamStaffResponse[];
  /** Whether a list refresh is in flight. */
  loading: boolean;
  /** Reloads the team's staff from the server. */
  refresh: () => Promise<void>;
  /** Adds a new staff member and refreshes the list. Returns the created row. */
  create: (
    request: ICreateTeamStaffRequest
  ) => Promise<ITeamStaffResponse>;
  /** Removes a staff member by id and refreshes the list. */
  remove: (id: GUID) => Promise<void>;
}

/**
 * Manages the technical staff (cuerpo técnico) of a single team, scoped to a
 * tournament (season) participation: loads the list, and creates/removes
 * entries. Standalone (no provider needed) so it can be dropped into both the
 * admin team page and the public team profile. Pass a falsy `teamId` or
 * `tournamentId` to keep it idle until both have resolved.
 * @param teamId - The team whose staff to manage.
 * @param tournamentId - The tournament (season participation) to scope by.
 */
export const useTeamStaff = (
  teamId: GUID | undefined,
  tournamentId: GUID | undefined
): UseTeamStaff => {
  const [staff, setStaff] = useState<ITeamStaffResponse[]>([]);
  const [loading, setLoading] = useState(false);

  const refresh = useCallback(async () => {
    if (!teamId || !tournamentId) {
      return;
    }
    setLoading(true);
    try {
      const response = await teamStaffService.getTeamStaffByTeamId(
        teamId,
        tournamentId
      );
      setStaff(response.data ?? []);
    } finally {
      setLoading(false);
    }
  }, [teamId, tournamentId]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const create = useCallback(
    async (request: ICreateTeamStaffRequest): Promise<ITeamStaffResponse> => {
      if (!teamId) {
        throw new Error('A team is required to add technical staff.');
      }
      const response = await teamStaffService.addTeamStaff(teamId, request);
      await refresh();
      return response.data;
    },
    [teamId, refresh]
  );

  const remove = useCallback(
    async (id: GUID): Promise<void> => {
      await teamStaffService.deleteTeamStaff(id);
      await refresh();
    },
    [refresh]
  );

  return { staff, loading, refresh, create, remove };
};
