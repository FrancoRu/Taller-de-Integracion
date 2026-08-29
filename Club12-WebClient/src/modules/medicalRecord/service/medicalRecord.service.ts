import { AxiosResponse } from 'axios';
import routes from '@/modules/core/constants/routes';
import { GUID } from '@/modules/core/types/types';
import {
  downloadfile,
  sendGet,
  sendPost,
  sendPut,
} from '@/modules/core/utils/axiosUtils';
import {
  IMedicalRecordResponse,
  IReviewMedicalRecordRequest,
  IUploadMedicalRecordRequest,
} from '@/modules/medicalRecord/type/medicalRecord';

/**
 * Service for managing player medical records and the resulting per-season
 * eligibility (HU-55/57/58/62). Every operation is scoped to the season
 * registration triple: player + team + tournament.
 */
export const medicalRecordService = {
  /**
   * Uploads a player's medical-record PDF (multipart) for a team and
   * tournament (HU-55).
   * @param {IUploadMedicalRecordRequest} request - Player, team, tournament and PDF file.
   * @returns {Promise<AxiosResponse<IMedicalRecordResponse>>} The resulting record (status Pending).
   */
  uploadMedicalRecord: async (
    request: IUploadMedicalRecordRequest
  ): Promise<AxiosResponse<IMedicalRecordResponse>> => {
    const formData = new FormData();
    formData.append('PlayerId', request.playerId);
    formData.append('TeamId', request.teamId);
    formData.append('TournamentId', request.tournamentId);
    formData.append('File', request.file);

    return await sendPost(routes.medicalRecords, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
  },

  /**
   * Approves or rejects a player's medical record (HU-58).
   * @param {IReviewMedicalRecordRequest} request - Player, team, tournament, decision and reason.
   * @returns {Promise<AxiosResponse<IMedicalRecordResponse>>} The resulting record.
   */
  reviewMedicalRecord: async (
    request: IReviewMedicalRecordRequest
  ): Promise<AxiosResponse<IMedicalRecordResponse>> =>
    await sendPut(`${routes.medicalRecords}/review`, request),

  /**
   * Retrieves the current medical-record / eligibility state of a player's
   * season registration (HU-62).
   * @param {GUID} playerId - The player.
   * @param {GUID} teamId - The team the player is registered to.
   * @param {GUID} tournamentId - The tournament (season).
   * @returns {Promise<AxiosResponse<IMedicalRecordResponse>>} The server response.
   */
  getMedicalRecord: async (
    playerId: GUID,
    teamId: GUID,
    tournamentId: GUID
  ): Promise<AxiosResponse<IMedicalRecordResponse>> =>
    await sendGet(routes.medicalRecords, { playerId, teamId, tournamentId }),

  /**
   * Downloads the stored ficha-médica PDF of a player's season registration
   * (HU-55/HU-56). The medical-records area is private, so the file is streamed
   * back through the API (as a blob) and saved locally rather than opened from a
   * public URL — the record's `fileUrl` is only an internal storage reference.
   * @param {GUID} playerId - The player.
   * @param {GUID} teamId - The team the player is registered to.
   * @param {GUID} tournamentId - The tournament (season).
   * @param {string} fileName - The name to save the downloaded PDF as.
   */
  downloadMedicalRecord: async (
    playerId: GUID,
    teamId: GUID,
    tournamentId: GUID,
    fileName: string
  ): Promise<void> => {
    const query = new URLSearchParams({
      playerId,
      teamId,
      tournamentId,
    }).toString();

    await downloadfile(`${routes.medicalRecords}/download?${query}`, fileName);
  },
};
