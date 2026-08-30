import { GUID, GenericResponsePagination } from '@/modules/core/types/types';
import { withTablePageSize } from '@/modules/core/constants/pagination';
import {
  IAddStageRequest,
  IPutStageRequest,
  IStageResponse,
  StageFiltered,
} from '@/modules/stage/type/stage';
import {
  sendDelete,
  sendGet,
  sendPost,
  sendPut,
} from '@/modules/core/utils/axiosUtils';
import routes from '@/modules/core/constants/routes';
import { AxiosResponse } from 'axios';

export const stageService = {
  /**
   * Creates a new stage.
   * @param {IAddStageRequest} stage - The data required to create a new stage.
   * @returns {Promise<AxiosResponse<IStageResponse>>} The response containing the created stage.
   */
  addStage: async (
    stage: IAddStageRequest
  ): Promise<AxiosResponse<IStageResponse>> =>
    sendPost<IStageResponse>(routes.stages, stage),

  /**
   * Updates a stage by its ID.
   * @param {GUID} id - The unique identifier of the stage to update.
   * @param {IPutStageRequest} stageRequest - The updated stage data.
   * @returns {Promise<AxiosResponse<IStageResponse>>} The response containing the updated stage.
   */
  putStageById: async (
    id: GUID,
    stageRequest: IPutStageRequest
  ): Promise<AxiosResponse<IStageResponse>> =>
    sendPut(`${routes.stages}/${id}`, stageRequest),

  /**
   * Retrieves a stage by its ID or its public slug.
   * @param {string} idOrSlug - The ID or slug of the stage to retrieve.
   * @returns {Promise<AxiosResponse<IStageResponse>>} The response containing the requested stage.
   */
  getStagesById: async (
    idOrSlug: string
  ): Promise<AxiosResponse<IStageResponse>> =>
    sendGet(`${routes.stages}/${idOrSlug}`),

  /**
   * Retrieves a paginated list of stages based on filters.
   * @param {StageFiltered} filter - The filter criteria for fetching stages.
   * @returns {Promise<AxiosResponse<GenericResponsePagination<IStageResponse>>>}
   * The response containing a paginated list of stages.
   */
  getStagesByFilters: async (
    filter: StageFiltered
  ): Promise<AxiosResponse<GenericResponsePagination<IStageResponse>>> =>
    sendGet(routes.stages, withTablePageSize(filter)),

  /**
   * Deletes a stage by its ID.
   * @param {GUID} id - The unique identifier of the stage to delete.
   * @returns {Promise<AxiosResponse<void>>} The response indicating the deletion result.
   */
  deleteStagesById: async (id: GUID): Promise<AxiosResponse<void>> =>
    sendDelete(`${routes.stages}/${id}`),

  /**
   * Generates stages automatically based on the provided ID.
   * @param {GUID} id - The unique identifier for which to generate stages.
   * @returns {Promise<AxiosResponse<IStageResponse[]>>} The response containing the generated stages.
   */
  generateStages: async (id: GUID): Promise<AxiosResponse<IStageResponse[]>> =>
    sendPost(`${routes.stages}/generate/${id}`),

  /**
   * Assigns one or more teams to a stage.
   * @param {GUID} id - The unique identifier of the stage.
   * @param {GUID[]} teamIds - The teams to assign (ignored when auto is true).
   * @param {boolean} auto - When true, fills available slots automatically.
   * @returns {Promise<AxiosResponse<void>>} The response confirming the assignment.
   */
  assignTeamsToStage: async (
    id: GUID,
    teamIds: GUID[],
    auto = false
  ): Promise<AxiosResponse<void>> =>
    sendPost(`${routes.stages}/${id}/assign-team`, { teamIds, auto }),

  /**
   * Unassigns one or more teams from a stage (removes them from that zone's
   * group). Used when moving a team to another zone or clearing its slot.
   * @param {GUID} id - The stage to remove the teams from.
   * @param {GUID[]} teamIds - The teams to unassign.
   * @returns {Promise<AxiosResponse<void>>} The response confirming the removal.
   */
  unassignTeamsFromStage: async (
    id: GUID,
    teamIds: GUID[]
  ): Promise<AxiosResponse<void>> =>
    sendDelete(`${routes.stages}/${id}/unassign-team`, undefined, { teamIds }),

  /**
   * Seeds an elimination stage's already-generated matches from the
   * division's group-stage standings.
   * @param {GUID} id - The elimination stage to seed.
   * @returns {Promise<AxiosResponse<void>>} The response confirming the seeding.
   */
  seedKnockoutStage: async (id: GUID): Promise<AxiosResponse<void>> =>
    sendPost(`${routes.stages}/${id}/seed`),
};
