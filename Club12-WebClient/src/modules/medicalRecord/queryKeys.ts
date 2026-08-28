import { GUID } from '@/modules/core/types/types';

/**
 * React Query keys for the medical-record module. A record is identified by
 * the season registration triple (player + team + tournament), so the by-key
 * query is keyed by all three ids.
 */
export const medicalRecordKeys = {
  byRegistration: (playerId: GUID, teamId: GUID, tournamentId: GUID) =>
    ['medicalRecord', 'byRegistration', playerId, teamId, tournamentId] as const,
};
