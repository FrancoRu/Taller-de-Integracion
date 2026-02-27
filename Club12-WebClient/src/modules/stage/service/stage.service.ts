import { GUID, GenericResponsePagination } from '@/modules/core/types/types';
import {
  IAddStageRequest,
  IPutStageRequest,
  IStageResponse,
  StageFiltered,
} from '../type/stage.d';
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
   * Retrieves a stage by its ID.
   * @param {GUID} id - The unique identifier of the stage to retrieve.
   * @returns {Promise<AxiosResponse<IStageResponse>>} The response containing the requested stage.
   */
  getStagesById: async (id: GUID): Promise<AxiosResponse<IStageResponse>> =>
    sendGet(`${routes.stages}/${id}`),

  /**
   * Retrieves a paginated list of stages based on filters.
   * @param {StageFiltered} filter - The filter criteria for fetching stages.
   * @returns {Promise<AxiosResponse<GenericResponsePagination<IStageResponse>>>}
   * The response containing a paginated list of stages.
   */
  getStagesByFilters: async (
    filter: StageFiltered
  ): Promise<AxiosResponse<GenericResponsePagination<IStageResponse>>> =>
    sendGet(routes.stages, filter),

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
};
