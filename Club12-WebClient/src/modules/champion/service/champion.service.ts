import { AxiosResponse } from 'axios';
import routes from '@/modules/core/constants/routes';
import { GUID } from '@/modules/core/types/types';
import { sendGet } from '@/modules/core/utils/axiosUtils';
import {
  IChampionHistory,
  IPodium,
} from '@/modules/champion/type/champion.d';

/**
 * Service for public champions/podium data (read-only).
 */
export const championService = {
  /**
   * Fetches the podium (top three per division) of a tournament by its id or
   * public slug. Returns one entry per division; a place is `null` until it is
   * decided.
   * @param {string} idOrSlug - Tournament id or slug.
   * @returns {Promise<AxiosResponse<IPodium[]>>} The server response.
   */
  getTournamentChampions: async (
    idOrSlug: string
  ): Promise<AxiosResponse<IPodium[]>> =>
    sendGet(`${routes.tournaments}/${idOrSlug}/champions`),

  /**
   * Fetches the public champions history (finished tournaments only),
   * optionally scoped to a single season.
   * @param {GUID} [seasonId] - Optional season to filter by.
   * @returns {Promise<AxiosResponse<IChampionHistory[]>>} The server response.
   */
  getChampionsHistory: async (
    seasonId?: GUID
  ): Promise<AxiosResponse<IChampionHistory[]>> =>
    sendGet(routes.champions, seasonId ? { seasonId } : undefined),
};
