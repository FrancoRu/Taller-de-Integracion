import {
  Filtered,
  GenericResponsePagination,
  GUID,
} from '@/modules/core/types/types';

/**
 * The sensitive, auditable actions tracked by the backend (HU-101). Persisted
 * as the enum name, so the frontend receives these exact strings.
 */
export type AuditAction =
  | 'DataWipe'
  | 'BackupRestore'
  | 'TournamentStatusChange'
  | 'PasswordReset'
  | 'PlayoffDraw';

/**
 * A single audit-trail entry as returned by `GET /api/audit-logs` (HU-101).
 * @interface IAuditLogResponse
 */
export interface IAuditLogResponse {
  /** The unique identifier of the audit entry. */
  id: GUID;

  /** The sensitive action that was performed (enum name). */
  action: string;

  /** Who performed the action (email, or "System"). */
  actor: string;

  /** The kind of entity targeted, when applicable. */
  targetType?: string | null;

  /** Identifier of the targeted entity, when applicable. */
  targetId?: string | null;

  /**
   * The target's human-readable name/label at the moment the action was
   * performed. Null for actions with no single named target, or entries
   * written before this field existed (fall back to targetId for those).
   */
  targetName?: string | null;

  /** Free-form human-readable context. */
  detail?: string | null;

  /** When the action happened (UTC). */
  timestamp: string;
}

/**
 * Filtering and pagination for the audit-trail listing (HU-101).
 * @interface AuditLogFiltered
 */
export interface AuditLogFiltered extends Filtered {
  /** Optional filter by the actor (who performed the action). */
  actor?: string;

  /** Optional filter by the action type. */
  action?: AuditAction;
}

/**
 * Context surface for reading the audit trail (HU-101).
 * @interface IAuditLogContextProps
 */
export interface IAuditLogContextProps {
  /**
   * Fetches audit entries (newest first) with pagination and optional filters.
   * @param filter The filter criteria to apply.
   */
  getAuditLogs(
    filter: AuditLogFiltered
  ): Promise<GenericResponsePagination<IAuditLogResponse> | void>;
}
