import { act, renderHook, waitFor } from '@testing-library/react';
import { AxiosError, AxiosResponse } from 'axios';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { useBackups } from '@/modules/backup/hook/backup.hook';
import { backupService } from '@/modules/backup/service/backup.service';
import type { IBackupRecordResponse } from '@/modules/backup/type/backup';

vi.mock('@/modules/backup/service/backup.service');

const mockedBackupService = vi.mocked(backupService, true);

const buildRecord = (
  overrides: Partial<IBackupRecordResponse> = {}
): IBackupRecordResponse => ({
  id: 'guid-1-aaaa-bbbb-cccc',
  createdAt: '2026-08-19T10:00:00Z',
  sizeBytes: 1024,
  origin: 'Manual',
  storagePath: 'backup-1.sql',
  ...overrides,
});

const buildResponse = <T,>(data: T, status = 200): AxiosResponse<T> =>
  ({
    data,
    status,
    statusText: 'OK',
    headers: {},
    config: {},
  }) as AxiosResponse<T>;

const buildAxiosError = (status: number): AxiosError =>
  ({
    isAxiosError: true,
    name: 'AxiosError',
    message: `Request failed with status code ${status}`,
    config: {},
    response: buildResponse(undefined, status),
    toJSON: () => ({}),
  }) as unknown as AxiosError;

beforeEach(() => {
  vi.clearAllMocks();
});

describe('useBackups — fetchBackups', () => {
  it('starts with an empty list and not loading', () => {
    const { result } = renderHook(() => useBackups());

    expect(result.current.backups).toEqual([]);
    expect(result.current.loading).toBe(false);
  });

  it('sets loading true during the fetch and populates backups on success', async () => {
    const records = [buildRecord()];
    let resolveFetch: (value: AxiosResponse<IBackupRecordResponse[]>) => void =
      () => {};
    mockedBackupService.getBackups.mockImplementation(
      () =>
        new Promise(resolve => {
          resolveFetch = resolve;
        })
    );

    const { result } = renderHook(() => useBackups());

    act(() => {
      void result.current.fetchBackups();
    });

    await waitFor(() => expect(result.current.loading).toBe(true));

    act(() => {
      resolveFetch(buildResponse(records));
    });

    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(result.current.backups).toEqual(records);
  });

  it('clears loading and leaves backups unchanged when the fetch fails', async () => {
    mockedBackupService.getBackups.mockRejectedValueOnce(
      buildAxiosError(500)
    );

    const { result } = renderHook(() => useBackups());

    await act(async () => {
      await result.current.fetchBackups();
    });

    expect(result.current.loading).toBe(false);
    expect(result.current.backups).toEqual([]);
  });
});

describe('useBackups — createBackup', () => {
  it('sets busy during the request, refetches the catalog, and resolves true on success', async () => {
    const created = buildRecord({ id: 'guid-new' });
    let resolveCreate: (value: AxiosResponse<IBackupRecordResponse>) => void =
      () => {};
    mockedBackupService.createBackup.mockImplementation(
      () =>
        new Promise(resolve => {
          resolveCreate = resolve;
        })
    );
    // The server applies retention pruning, so the hook must re-read the
    // authoritative catalog instead of optimistically prepending the new row.
    mockedBackupService.getBackups.mockResolvedValue(buildResponse([created]));

    const { result } = renderHook(() => useBackups());

    let createPromise: Promise<boolean>;
    act(() => {
      createPromise = result.current.createBackup();
    });

    await waitFor(() => expect(result.current.busy).toBe(true));

    await act(async () => {
      resolveCreate(buildResponse(created));
      await createPromise;
    });

    await expect(createPromise!).resolves.toBe(true);
    expect(result.current.busy).toBe(false);
    expect(mockedBackupService.getBackups).toHaveBeenCalled();
    expect(result.current.backups).toEqual([created]);
  });

  it('resolves false and leaves the list unchanged when the server is busy (409)', async () => {
    mockedBackupService.createBackup.mockRejectedValueOnce(
      buildAxiosError(409)
    );

    const { result } = renderHook(() => useBackups());

    let created: boolean = true;
    await act(async () => {
      created = await result.current.createBackup();
    });

    expect(created).toBe(false);
    expect(result.current.busy).toBe(false);
    expect(result.current.backups).toEqual([]);
  });
});

describe('useBackups — deleteBackup', () => {
  it('removes the deleted record from the list and resolves true on success', async () => {
    const record = buildRecord();
    mockedBackupService.getBackups.mockResolvedValueOnce(
      buildResponse([record])
    );
    mockedBackupService.deleteBackup.mockResolvedValueOnce(
      buildResponse(undefined, 204)
    );

    const { result } = renderHook(() => useBackups());
    await act(async () => {
      await result.current.fetchBackups();
    });

    let deleted: boolean = false;
    await act(async () => {
      deleted = await result.current.deleteBackup(record.id);
    });

    expect(deleted).toBe(true);
    expect(result.current.backups).toEqual([]);
  });

  it('resolves false and leaves the list unchanged when the record no longer exists (404)', async () => {
    const record = buildRecord();
    mockedBackupService.getBackups.mockResolvedValueOnce(
      buildResponse([record])
    );
    mockedBackupService.deleteBackup.mockRejectedValueOnce(
      buildAxiosError(404)
    );

    const { result } = renderHook(() => useBackups());
    await act(async () => {
      await result.current.fetchBackups();
    });

    let deleted: boolean = true;
    await act(async () => {
      deleted = await result.current.deleteBackup(record.id);
    });

    expect(deleted).toBe(false);
    expect(result.current.backups).toEqual([record]);
  });
});

describe('useBackups — restoreBackup', () => {
  it('refetches the catalog after a successful restore and resolves true', async () => {
    // A restore replays a full-schema dump, so the real catalog reverts to the
    // restored snapshot's state — the hook must re-read it, never trust the
    // pre-restore safety-backup record the endpoint returns.
    const restoredCatalog = [buildRecord({ id: 'guid-from-snapshot' })];
    mockedBackupService.restoreBackup.mockResolvedValueOnce(
      buildResponse(buildRecord({ id: 'guid-safety', origin: 'Job' }))
    );
    mockedBackupService.getBackups.mockResolvedValueOnce(
      buildResponse(restoredCatalog)
    );

    const { result } = renderHook(() => useBackups());

    let restored: boolean = false;
    await act(async () => {
      restored = await result.current.restoreBackup('guid-target');
    });

    expect(restored).toBe(true);
    expect(mockedBackupService.getBackups).toHaveBeenCalled();
    expect(result.current.backups).toEqual(restoredCatalog);
  });

  it('sets busy true while in flight and resolves false on failure (500)', async () => {
    let rejectRestore: (error: AxiosError) => void = () => {};
    mockedBackupService.restoreBackup.mockImplementation(
      () =>
        new Promise((_, reject) => {
          rejectRestore = reject;
        })
    );

    const { result } = renderHook(() => useBackups());

    let restorePromise: Promise<boolean>;
    act(() => {
      restorePromise = result.current.restoreBackup('guid-target');
    });

    await waitFor(() => expect(result.current.busy).toBe(true));

    await act(async () => {
      rejectRestore(buildAxiosError(500));
      await restorePromise;
    });

    await expect(restorePromise!).resolves.toBe(false);
    expect(result.current.busy).toBe(false);
    expect(result.current.backups).toEqual([]);
  });
});
