import { GUID } from '@/modules/core/types/types';
import { MedicalRecordStatus } from '@/modules/core/enum/medicalRecord/medicalRecordStatus';

/**
 * The medical-record / eligibility state of a player's season registration
 * (player + team + tournament). Mirrors the backend `MedicalRecordResponse`
 * (HU-55/57/58/62).
 * @interface IMedicalRecordResponse
 */
export interface IMedicalRecordResponse {
  playerId: GUID;
  teamId: GUID;
  tournamentId: GUID;

  /** The medical-record status (Pending / Approved / Rejected). */
  status: MedicalRecordStatus;

  /** True only when the record is Approved (HU-57). */
  isHabilitado: boolean;

  /** Storage reference of the uploaded file, or null if none yet. */
  fileUrl?: string | null;

  /** Original uploaded file name, or null if none yet. */
  fileName?: string | null;

  /** Reason recorded on rejection, if any. */
  reviewReason?: string | null;

  /** When the record was last approved/rejected, if ever. */
  reviewedAt?: string | null;
}

/**
 * Multipart request to upload a player's medical-record file (PDF) for a
 * specific team and tournament (HU-55).
 * @interface IUploadMedicalRecordRequest
 */
export interface IUploadMedicalRecordRequest {
  playerId: GUID;
  teamId: GUID;
  tournamentId: GUID;
  /** The medical-record file (PDF) to upload. */
  file: File;
}

/**
 * Owner/admin request to approve or reject a player's medical record for a
 * team and tournament (HU-58).
 * @interface IReviewMedicalRecordRequest
 */
export interface IReviewMedicalRecordRequest {
  playerId: GUID;
  teamId: GUID;
  tournamentId: GUID;
  /** True to approve (player becomes habilitado); false to reject. */
  approve: boolean;
  /** Optional reason, typically recorded when rejecting. */
  reason?: string;
}

/**
 * Context properties and methods for managing player medical records and the
 * resulting per-season eligibility (HU-55/57/58/62).
 * @interface IMedicalRecordContextProps
 */
export interface IMedicalRecordContextProps {
  /**
   * Uploads a player's medical-record PDF for a team and tournament (HU-55).
   * The record starts Pending until reviewed.
   */
  uploadMedicalRecord(
    request: IUploadMedicalRecordRequest
  ): Promise<IMedicalRecordResponse | void>;

  /**
   * Approves or rejects a player's medical record (HU-58). Approving makes the
   * player habilitado (HU-57); rejecting leaves them not-habilitado.
   */
  reviewMedicalRecord(
    request: IReviewMedicalRecordRequest
  ): Promise<IMedicalRecordResponse | void>;

  /**
   * Fetches the current medical-record / eligibility state of a player's
   * season registration (HU-62), or void when none exists.
   */
  getMedicalRecord(
    playerId: GUID,
    teamId: GUID,
    tournamentId: GUID
  ): Promise<IMedicalRecordResponse | void>;
}
