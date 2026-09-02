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

  // Pull the authoritative catalog from the server. Errors are swallowed: the
  // caller keeps the previous list and page-level notify* handles messaging.
  const refreshCatalog = useCallback(async (): Promise<void> => {
    try {
      const response = await backupService.getBackups();
      setBackups(response.data);
    } catch {
      // keep the previous list
    }
  }, []);

  const fetchBackups = useCallback(async (): Promise<void> => {
    setLoading(true);
    try {
      await refreshCatalog();
    } finally {
      setLoading(false);
    }
  }, [refreshCatalog]);

  const createBackup = useCallback(async (): Promise<boolean> => {
    setBusy(true);
    try {
      await backupService.createBackup();
      // Refetch, not an optimistic prepend: a manual backup applies server-side
      // retention pruning, so the new row is not the only change to the catalog.
      await refreshCatalog();
      return true;
    } catch {
      return false;
    } finally {
      setBusy(false);
    }
  }, [refreshCatalog]);

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
      await backupService.restoreBackup(id);
      // Refetch, never optimistic: a restore replays a full-schema dump
      // (BackupRecords included), so the catalog reverts to the restored
      // snapshot's state — later backups, later deletions and the just-created
      // pre-restore safety backup are all gone from the real table.
      await refreshCatalog();
      return true;
    } catch {
      return false;
    } finally {
      setBusy(false);
    }
  }, [refreshCatalog]);

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
