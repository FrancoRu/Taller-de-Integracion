import { AxiosResponse } from 'axios';
import routes from '@/modules/core/constants/routes';
import { GUID } from '@/modules/core/types/types';
import { sendDelete, sendGet, sendPost } from '@/modules/core/utils/axiosUtils';
import {
  ICreateTeamStaffRequest,
  ITeamStaffResponse,
} from '@/modules/teamStaff/type/teamStaff';

/**
 * Service for the team technical-staff (cuerpo técnico) endpoints. Adding and
 * deleting require AdminOrOwner; listing is public.
 */
export const teamStaffService = {
  /**
   * Adds a technical staff member to a team for a given tournament.
   * @param {GUID} teamId - The team the staff member belongs to.
   * @param {ICreateTeamStaffRequest} request - The name, role and tournament.
   * @returns {Promise<AxiosResponse<ITeamStaffResponse>>} The created staff member.
   */
  addTeamStaff: async (
    teamId: GUID,
    request: ICreateTeamStaffRequest
  ): Promise<AxiosResponse<ITeamStaffResponse>> =>
    sendPost<ITeamStaffResponse>(
      `${routes.teams}/${teamId}/${routes.staff}`,
      request
    ),

  /**
   * Lists a team's technical staff for a given tournament.
   * @param {GUID} teamId - The team whose staff to list.
   * @param {GUID} tournamentId - The tournament (season participation) to scope by.
   * @returns {Promise<AxiosResponse<ITeamStaffResponse[]>>} The staff members.
   */
  getTeamStaffByTeamId: async (
    teamId: GUID,
    tournamentId: GUID
  ): Promise<AxiosResponse<ITeamStaffResponse[]>> =>
    sendGet<ITeamStaffResponse[]>(
      `${routes.teams}/${teamId}/${routes.staff}`,
      { tournamentId }
    ),

  /**
   * Removes a technical staff member by its id.
   * @param {GUID} id - The id of the staff member to remove.
   * @returns {Promise<AxiosResponse<void>>} The server response.
   */
  deleteTeamStaff: async (id: GUID): Promise<AxiosResponse<void>> =>
    sendDelete<void>(`${routes.staff}/${id}`),
};
