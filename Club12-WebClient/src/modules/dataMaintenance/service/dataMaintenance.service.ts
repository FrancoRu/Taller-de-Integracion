import { AxiosResponse } from 'axios';
import routes from '@/modules/core/constants/routes';
import { sendPost } from '@/modules/core/utils/axiosUtils';
import {
  IDataSeedResult,
  IDataWipeResult,
} from '@/modules/dataMaintenance/type/dataMaintenance';

/**
 * Admin-only tools for resetting tournament-domain data to a clean,
 * realistic sample state.
 */
export const dataMaintenanceService = {
  /**
   * Deletes every tournament-domain row. Identity is untouched.
   * @returns {Promise<AxiosResponse<IDataWipeResult>>} Row counts removed.
   */
  wipeSampleData: async (): Promise<AxiosResponse<IDataWipeResult>> =>
    await sendPost(`${routes.dataMaintenance}/wipe`),

  /**
   * Seeds 2 complete sample tournaments. Rejects with a 409 response if
   * the database already has tournament data.
   * @returns {Promise<AxiosResponse<IDataSeedResult>>} Row counts created.
   */
  seedSampleData: async (): Promise<AxiosResponse<IDataSeedResult>> =>
    await sendPost(`${routes.dataMaintenance}/seed`),
};
