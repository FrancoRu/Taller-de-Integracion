import { useCallback, useState } from 'react';
import { backupService } from '@/modules/backup/service/backup.service';
import { IBackupRecordResponse } from '@/modules/backup/type/backup';

export interface UseBackupsResult {
  backups: IBackupRecordResponse[];
  loading: boolean;
  busy: boolean;
  fetchBackups: () => Promise<void>;
  createBackup: () => Promise<boolean>;
  deleteBackup: (id: string) => Promise<boolean>;
  restoreBackup: (id: string) => Promise<boolean>;
}

/**
 * Plain state hook (not a context provider) owning the backup catalog for
 * the admin panel. `BackupsTable` is currently its only consumer, so a
 * shared context/provider would add indirection nothing else needs.
 */
export const useBackups = (): UseBackupsResult => {
  const [backups, setBackups] = useState<IBackupRecordResponse[]>([]);
  const [loading, setLoading] = useState(false);
  const [busy, setBusy] = useState(false);

  const fetchBackups = useCallback(async (): Promise<void> => {
    setLoading(true);
    try {
      const response = await backupService.getBackups();
      setBackups(response.data);
    } catch {
      // Swallow: caller keeps the previous list, page-level error handling
      // (toast/notify) is the consumer's responsibility.
    } finally {
      setLoading(false);
    }
  }, []);

  const createBackup = useCallback(async (): Promise<boolean> => {
    setBusy(true);
    try {
      const response = await backupService.createBackup();
      setBackups(prev => [response.data, ...prev]);
      return true;
    } catch {
      return false;
    } finally {
      setBusy(false);
    }
  }, []);

  const deleteBackup = useCallback(async (id: string): Promise<boolean> => {
    setBusy(true);
    try {
      await backupService.deleteBackup(id);
      setBackups(prev => prev.filter(backup => backup.id !== id));
      return true;
    } catch {
      return false;
    } finally {
      setBusy(false);
    }
  }, []);

  const restoreBackup = useCallback(async (id: string): Promise<boolean> => {
    setBusy(true);
    try {
      const response = await backupService.restoreBackup(id);
      setBackups(prev => [response.data, ...prev]);
      return true;
    } catch {
      return false;
    } finally {
      setBusy(false);
    }
  }, []);

  return {
    backups,
    loading,
    busy,
    fetchBackups,
    createBackup,
    deleteBackup,
    restoreBackup,
  };
};
