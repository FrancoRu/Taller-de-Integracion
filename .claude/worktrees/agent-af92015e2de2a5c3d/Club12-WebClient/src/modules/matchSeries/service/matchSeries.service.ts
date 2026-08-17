import { GUID, GenericResponsePagination } from '@/modules/core/types/types';
import { withTablePageSize } from '@/modules/core/constants/pagination';
import {
  IAddGameToSeriesRequest,
  IAddMatchSeriesRequest,
  IMatchSeriesResponse,
  ISeriesGameResponse,
  MatchSeriesFiltered,
} from '@/modules/matchSeries/type/matchSeries.d';
import { sendGet, sendPost } from '@/modules/core/utils/axiosUtils';
import routes from '@/modules/core/constants/routes';
import { AxiosResponse } from 'axios';

export const matchSeriesService = {
  /**
   * Creates a new best-of-N series between two teams at a stage.
   * @param {IAddMatchSeriesRequest} series - The data required to create the series.
   * @returns {Promise<AxiosResponse<IMatchSeriesResponse>>} The response containing the created series.
   */
  addMatchSeries: async (
    series: IAddMatchSeriesRequest
  ): Promise<AxiosResponse<IMatchSeriesResponse>> =>
    sendPost<IMatchSeriesResponse>(routes.matchSeries, series),

  /**
   * Retrieves a series by its ID.
   * @param {GUID} id - The unique identifier of the series to retrieve.
   * @returns {Promise<AxiosResponse<IMatchSeriesResponse>>} The response containing the requested series.
   */
  getMatchSeriesById: async (
    id: GUID
  ): Promise<AxiosResponse<IMatchSeriesResponse>> =>
    sendGet(`${routes.matchSeries}/${id}`),

  /**
   * Retrieves a paginated list of series based on filters.
   * @param {MatchSeriesFiltered} filter - The filter criteria for fetching series.
   * @returns {Promise<AxiosResponse<GenericResponsePagination<IMatchSeriesResponse>>>}
   * The response containing a paginated list of series.
   */
  getMatchSeriesByFilters: async (
    filter: MatchSeriesFiltered
  ): Promise<AxiosResponse<GenericResponsePagination<IMatchSeriesResponse>>> =>
    sendGet(routes.matchSeries, withTablePageSize(filter)),

  /**
   * Schedules the next game of an existing series.
   * @param {GUID} id - The unique identifier of the series to add a game to.
   * @param {IAddGameToSeriesRequest} game - The game scheduling data.
   * @returns {Promise<AxiosResponse<ISeriesGameResponse>>} The response containing the created game.
   */
  addGameToSeries: async (
    id: GUID,
    game: IAddGameToSeriesRequest
  ): Promise<AxiosResponse<ISeriesGameResponse>> =>
    sendPost(`${routes.matchSeries}/${id}/games`, game),
};
