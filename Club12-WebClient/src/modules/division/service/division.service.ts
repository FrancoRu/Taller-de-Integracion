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
  AddDivisionRequest,
  DivisionFiltered,
  EnrollTeamsRequest,
  IDivisionResponse,
  IPutDivisionRequest,
  ReassignTeamToSubGroupRequest,
  RebuildSubGroupsRequest,
  UnenrollTeamsRequest,
} from '@/modules/division/type/division';
import { ITeamResponse } from '@/modules/team/type/team.d';
import { IStageResponse } from '@/modules/stage/type/stage';

/**
 * DivisionService provides methods to interact with the divisions API.
 */
export const divisionService = {
  /**
   * Adds a new division.
   * @param {AddDivisionRequest} division - The division data to be added.
   * @returns {Promise<AxiosResponse<IDivisionResponse>>} - A promise that resolves with the server response.
   */
  addDivision: async (
    division: AddDivisionRequest
  ): Promise<AxiosResponse<IDivisionResponse>> =>
    sendPost<IDivisionResponse>(routes.divisions, division),

  /**
   * Generates the fixture for a division based on its ID.
   * @param {string} id - The ID of the division to generate the fixture for.
   * @returns {Promise<AxiosResponse<IDivisionResponse>>} - A promise that resolves with the server response.
   */
  generateFixtureByDivisionId: async (id: GUID): Promise<AxiosResponse<void>> =>
    sendPost<void>(`${routes.divisions}/${id}/generate-fixture`),

  /**
   * Updates an existing division by its ID.
   * @param {string} id - The ID of the division to be updated.
   * @param {IPutDivisionRequest} division - The updated division data.
   * @returns {Promise<AxiosResponse<IDivisionResponse>>} - A promise that resolves with the server response.
   */
  putDivisionById: async (
    id: GUID,
    division: IPutDivisionRequest
  ): Promise<AxiosResponse<IDivisionResponse>> =>
    sendPut<IDivisionResponse>(`${routes.divisions}/${id}`, division),

  /**
   * Retrieves a division by its ID or its public slug.
   * @param {string} idOrSlug - The ID or slug of the division to retrieve.
   * @returns {Promise<AxiosResponse<IDivisionResponse>>} - A promise that resolves with the division data.
   */
  getDivisionsById: async (
    idOrSlug: string
  ): Promise<AxiosResponse<IDivisionResponse>> =>
    sendGet<IDivisionResponse>(`${routes.divisions}/${idOrSlug}/detail`),

  /**
   * Retrieves divisions based on provided filters.
   * @param {DivisionFiltered} filter - The filters to apply when retrieving divisions.
   * @returns {Promise<AxiosResponse<IDivisionResponse>>} - A promise that resolves with a list of divisions matching the filter.
   */
  getDivisionsByFilters: async (
    filter: DivisionFiltered
  ): Promise<AxiosResponse<GenericResponsePagination<IDivisionResponse>>> =>
    sendGet<GenericResponsePagination<IDivisionResponse>>(
      routes.divisions,
      withTablePageSize(filter)
    ),

  /**
   * Deletes a division by its ID.
   * @param {string} id - The ID of the division to delete.
   * @returns {Promise<AxiosResponse<IDivisionResponse>>} - A promise that resolves when the division is deleted.
   */
  deleteDivisionsById: async (id: GUID): Promise<AxiosResponse<void>> =>
    sendDelete<void>(`${routes.divisions}/${id}`),

  /**
   * Fetches every team enrolled in a division's roster.
   * @param {GUID} divisionId - The division whose roster to fetch.
   * @returns {Promise<AxiosResponse<ITeamResponse[]>>} - The enrolled teams.
   */
  getRoster: async (divisionId: GUID): Promise<AxiosResponse<ITeamResponse[]>> =>
    sendGet<ITeamResponse[]>(`${routes.divisions}/${divisionId}/roster`),

  /**
   * Enrols one or more teams in a division's roster.
   * @param {GUID} divisionId - The division to enrol the teams into.
   * @param {GUID[]} teamIds - The teams to enrol.
   * @returns {Promise<AxiosResponse<ITeamResponse[]>>} - The division's roster as it stands after enrolling (200).
   */
  enrollTeams: async (
    divisionId: GUID,
    teamIds: GUID[]
  ): Promise<AxiosResponse<ITeamResponse[]>> =>
    sendPost<ITeamResponse[]>(`${routes.divisions}/${divisionId}/roster`, {
      teamIds,
    } satisfies EnrollTeamsRequest),

  /**
   * Removes one or more teams from a division's roster, cascading to any
   * stage placement they still hold within the division.
   * @param {GUID} divisionId - The division to unenrol the teams from.
   * @param {GUID[]} teamIds - The teams to unenrol.
   * @returns {Promise<AxiosResponse<void>>} - The response confirming removal.
   */
  unenrollTeams: async (
    divisionId: GUID,
    teamIds: GUID[]
  ): Promise<AxiosResponse<void>> =>
    sendDelete<void>(`${routes.divisions}/${divisionId}/roster`, undefined, {
      teamIds,
    } satisfies UnenrollTeamsRequest),

  /**
   * Clears the division's current sub-group placements and re-runs a
   * balanced random distribution over its whole roster.
   * @param {GUID} divisionId - The division whose sub-groups to auto-distribute.
   * @returns {Promise<AxiosResponse<void>>} - The response confirming the redistribution.
   */
  autoDistribute: async (divisionId: GUID): Promise<AxiosResponse<void>> =>
    sendPost<void>(`${routes.divisions}/${divisionId}/roster/auto-distribute`),

  /**
   * Rebuilds a division's sub-group stage layer to a new count, keeping the
   * roster untouched.
   * @param {GUID} divisionId - The division whose sub-group count to change.
   * @param {number} subGroupCount - The new sub-group count.
   * @returns {Promise<AxiosResponse<IStageResponse[]>>} - The newly-built sub-group stages (200).
   */
  rebuildSubGroups: async (
    divisionId: GUID,
    subGroupCount: number
  ): Promise<AxiosResponse<IStageResponse[]>> =>
    sendPost<IStageResponse[]>(`${routes.divisions}/${divisionId}/sub-groups/rebuild`, {
      subGroupCount,
    } satisfies RebuildSubGroupsRequest),

  /**
   * Manually moves one enrolled team from one sub-group to another within
   * the same division, without touching any other team's placement.
   * @param {GUID} divisionId - The division the two sub-groups belong to.
   * @param {GUID} teamId - The team to move.
   * @param {GUID} fromStageId - The sub-group stage the team currently belongs to.
   * @param {GUID} toStageId - The sub-group stage to move the team into.
   * @returns {Promise<AxiosResponse<void>>} - The response confirming the move.
   */
  reassignTeamToSubGroup: async (
    divisionId: GUID,
    teamId: GUID,
    fromStageId: GUID,
    toStageId: GUID
  ): Promise<AxiosResponse<void>> =>
    sendPost<void>(`${routes.divisions}/${divisionId}/sub-groups/reassign`, {
      teamId,
      fromStageId,
      toStageId,
    } satisfies ReassignTeamToSubGroupRequest),
};
