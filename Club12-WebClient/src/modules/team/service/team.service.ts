import { AxiosResponse } from 'axios';
import routes from '@/modules/core/constants/routes';
import { withTablePageSize } from '@/modules/core/constants/pagination';
import { GenericResponsePagination, GUID } from '@/modules/core/types/types';
import {
  sendDelete,
  sendGet,
  sendPost,
  sendPut,
} from '@/modules/core/utils/axiosUtils';
import {
  IAddTeamRequest,
  IPutTeamRequest,
  TeamFiltered,
  ITeamResponse,
} from '@/modules/team/type/team';

/**
 * Service for managing teams.
 */
export const teamService = {
  /**
   * Adds a new team.
   * @param {IAddTeamRequest} team - The team details to add.
   * @returns {Promise<AxiosResponse<ITeamResponse>>} The server response.
   */
  addTeam: async (
    team: IAddTeamRequest
  ): Promise<AxiosResponse<ITeamResponse>> => {
    const formData = new FormData();
    formData.append('Name', team.name);
    formData.append('ThreeLetterCode', team.threeLetterCode);
    formData.append('ShirtColor', team.shirtColor);
    if (team.shirtSecondaryColor) {
      formData.append('ShirtSecondaryColor', team.shirtSecondaryColor);
    }
    if (team.jerseyStyle) {
      formData.append('JerseyStyle', team.jerseyStyle);
    }
    formData.append('LogoFile', team.logo);

    if (team.tournamentId !== undefined) {
      formData.append('TournamentId', team.tournamentId ?? '');
    }

    return await sendPost(routes.teams, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
  },

  /**
   * Updates an existing team.
   * @param {string} id - The ID of the team to update.
   * @param {IPutTeamRequest} data - The updated team details.
   * @returns {Promise<AxiosResponse<ITeamResponse>>} The server response.
   */
  putTeamById: async (
    id: GUID,
    data: IPutTeamRequest
  ): Promise<AxiosResponse<ITeamResponse>> =>
    await sendPut(`${routes.teams}/${id}`, data),

  /**
   * Updates the logo for a specific team.
   * @param {string} id - The ID of the team to update.
   * @param {File} logo - The new logo file.
   * @returns {Promise<AxiosResponse<void>>} The server response.
   */
  putTeamLogoById: async (id: GUID, logo: File): Promise<AxiosResponse<void>> => {
    // The backend binds [FromForm] UpdateTeamLogoRequest, which requires both a
    // TeamId and the LogoFile — send a proper multipart body (a raw File as the
    // request body fails model binding with a 400).
    const formData = new FormData();
    formData.append('TeamId', id);
    formData.append('LogoFile', logo);
    return await sendPut(`${routes.teams}/${id}/logo`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  /**
   * Retrieves teams based on the provided filters.
   * @param {TeamFiltered} filters - The filters to apply when retrieving teams.
   * @returns {Promise<AxiosResponse<GenericResponsePagination<ITeamResponse>>>} The server response containing the filtered teams.
   */
  getTeamsByFiltered: async (
    filters: TeamFiltered
  ): Promise<AxiosResponse<GenericResponsePagination<ITeamResponse>>> =>
    await sendGet(routes.teams, withTablePageSize(filters)),

  /**
   * Retrieves a specific team by its ID or its public slug.
   * @param {string} idOrSlug - The ID or slug of the team to retrieve.
   * @returns {Promise<AxiosResponse<ITeamResponse>>} The server response containing the team details.
   */
  getTeamById: async (
    idOrSlug: string
  ): Promise<AxiosResponse<ITeamResponse>> =>
    await sendGet(`${routes.teams}/${idOrSlug}`),

  /**
   * Deletes a team by its ID.
   * @param {string} id - The ID of the team to delete.
   * @returns {Promise<AxiosResponse<void>>} The server response.
   */
  deleteTeamById: async (id: GUID): Promise<AxiosResponse<void>> =>
    await sendDelete(`${routes.teams}/${id}`),
};
