import { AxiosResponse } from 'axios';
import routes from '@/modules/core/constants/routes';
import { sendDelete, sendGet, sendPost } from '@/modules/core/utils/axiosUtils';
import { IBackupRecordResponse } from '@/modules/backup/type/backup';

/**
 * Admin-only tools for generating, listing, deleting and restoring database
 * backups, plus the escape hatch for the maintenance-mode window a restore
 * opens.
 */
export const backupService = {
  /**
   * Retrieves every catalogued backup, newest first.
   * @returns {Promise<AxiosResponse<IBackupRecordResponse[]>>} The server response.
   */
  getBackups: async (): Promise<AxiosResponse<IBackupRecordResponse[]>> =>
    await sendGet(routes.backups),

  /**
   * Triggers an on-demand (manual) backup.
   * @returns {Promise<AxiosResponse<IBackupRecordResponse>>} The server response containing the new backup.
   */
  createBackup: async (): Promise<AxiosResponse<IBackupRecordResponse>> =>
    await sendPost(routes.backups),

  /**
   * Deletes a catalogued backup by its ID.
   * @param {string} id - The ID of the backup to delete.
   * @returns {Promise<AxiosResponse<void>>} The server response.
   */
  deleteBackup: async (id: string): Promise<AxiosResponse<void>> =>
    await sendDelete(`${routes.backups}/${id}`),

  /**
   * Restores the database from a catalogued backup. The server takes an
   * automatic safety backup of the current state first and returns it.
   * @param {string} id - The ID of the backup to restore from.
   * @returns {Promise<AxiosResponse<IBackupRecordResponse>>} The server response containing the safety backup.
   */
  restoreBackup: async (
    id: string
  ): Promise<AxiosResponse<IBackupRecordResponse>> =>
    await sendPost(`${routes.backups}/${id}/restore`),

  /**
   * Force-exits maintenance mode, in case it is stuck active.
   * @returns {Promise<AxiosResponse<void>>} The server response.
   */
  exitMaintenance: async (): Promise<AxiosResponse<void>> =>
    await sendDelete(routes.maintenance),
};
