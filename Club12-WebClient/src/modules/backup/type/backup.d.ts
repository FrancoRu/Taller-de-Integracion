/**
 * A single catalogued backup, as returned by the backend catalog. Mirrors
 * `Application/DTOs/Backup/Response/BackupRecordResponse.cs`.
 * @interface IBackupRecordResponse
 */
export interface IBackupRecordResponse {
  /**
   * The unique identifier of the backup record.
   * @type {string}
   */
  id: string;

  /**
   * ISO timestamp of when the backup was created (Fecha).
   * @type {string}
   */
  createdAt: string;

  /**
   * The size of the backup file in bytes (Peso).
   * @type {number}
   */
  sizeBytes: number;

  /**
   * How the backup was created (Forma de creación): a manual on-demand
   * request or an automated scheduled job.
   * @type {'Manual' | 'Job'}
   */
  origin: 'Manual' | 'Job';

  /**
   * The storage key used to locate the backup file.
   * @type {string}
   */
  storagePath: string;
}

/**
 * Current maintenance-mode state, as returned by the backend. Mirrors
 * `Application/DTOs/Backup/Response/MaintenanceStatusResponse.cs`.
 * @interface IMaintenanceStatusResponse
 */
export interface IMaintenanceStatusResponse {
  /**
   * Whether the database is currently in maintenance mode.
   * @type {boolean}
   */
  isActive: boolean;

  /**
   * The reason maintenance mode was entered, if active.
   * @type {string | null}
   */
  reason: string | null;

  /**
   * ISO timestamp of when maintenance mode was entered, if active.
   * @type {string | null}
   */
  enteredAtUtc: string | null;
}
