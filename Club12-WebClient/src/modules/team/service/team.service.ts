import { AxiosResponse } from 'axios';
import routes from '../../core/constants/routes';
import { GenericResponsePagination } from '../../core/types/types';
import {
  sendDelete,
  sendGet,
  sendPost,
  sendPut,
} from '../../core/utils/axiosUtils';
import {
  AddTeamRequest,
  PutTeamRequest,
  TeamFiltered,
  TeamResponse,
} from '../type/team';

/**
 * Service for managing teams.
 */
export const teamService = {
  /**
   * Adds a new team.
   * @param {AddTeamRequest} team - The team details to add.
   * @returns {Promise<AxiosResponse<TeamResponse>>} The server response.
   */
  addTeam: async (team: AddTeamRequest): Promise<AxiosResponse<TeamResponse>> =>
    await sendPost(routes.teams, team),

  /**
   * Adds a batch of teams to a division.
   * @param {string} divisionId - The ID of the division to add teams to.
   * @param {File} teamFile - The file containing team data.
   * @param {File} logoFile - The file containing team logos.
   * @returns {Promise<AxiosResponse<TeamResponse>>} The server response.
   */
  addTeamToDivisionIdBatch: async (
    divisionId: string,
    teamFile: File,
    logoFile: File
  ): Promise<AxiosResponse<TeamResponse>> =>
    await sendPost(`${routes.teams}/${divisionId}`, { teamFile, logoFile }),

  /**
   * Updates an existing team.
   * @param {string} id - The ID of the team to update.
   * @param {PutTeamRequest} data - The updated team details.
   * @returns {Promise<AxiosResponse<TeamResponse>>} The server response.
   */
  putTeamById: async (
    id: string,
    data: PutTeamRequest
  ): Promise<AxiosResponse<TeamResponse>> =>
    await sendPut(`${routes.teams}/${id}`, data),

  /**
   * Updates the logo for a specific team.
   * @param {string} id - The ID of the team to update.
   * @param {File} logo - The new logo file.
   * @returns {Promise<AxiosResponse<void>>} The server response.
   */
  putTeamLogoById: async (
    id: string,
    logo: File
  ): Promise<AxiosResponse<void>> =>
    await sendPut(`${routes.teams}/${id}/logo`, logo),

  /**
   * Retrieves teams based on the provided filters.
   * @param {TeamFiltered} filters - The filters to apply when retrieving teams.
   * @returns {Promise<AxiosResponse<GenericResponsePagination<TeamResponse>>>} The server response containing the filtered teams.
   */
  getTeamsByFiltered: async (
    filters: TeamFiltered
  ): Promise<AxiosResponse<GenericResponsePagination<TeamResponse>>> =>
    await sendGet(routes.teams, filters),

  /**
   * Retrieves a specific team by its ID.
   * @param {string} id - The ID of the team to retrieve.
   * @returns {Promise<AxiosResponse<TeamResponse>>} The server response containing the team details.
   */
  getTeamById: async (id: string): Promise<AxiosResponse<TeamResponse>> =>
    await sendGet(`${routes.teams}/${id}`),

  /**
   * Deletes a team by its ID.
   * @param {string} id - The ID of the team to delete.
   * @returns {Promise<AxiosResponse<void>>} The server response.
   */
  deleteTeamById: async (id: string): Promise<AxiosResponse<void>> =>
    await sendDelete(`${routes.teams}/${id}`),
};
