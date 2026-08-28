import { AxiosError } from 'axios';
import { HttpStatus } from '@/modules/core/constants/httpStatus';

/**
 * User-facing (Spanish) messages for the three roster invariants the backend
 * enforces on registration, returned as a 409 Conflict (HU-54).
 */
export const ROSTER_CONFLICT_MESSAGES = {
  duplicateDorsal:
    'Ese dorsal ya está asignado a otro jugador del equipo en esta temporada.',
  rosterFull: 'El plantel ya alcanzó el máximo de jugadores permitido.',
  alreadyInAnotherTeam:
    'El jugador ya está inscripto en otro equipo de este torneo.',
  generic: 'No se pudo asignar el dorsal. Intentá nuevamente.',
} as const;

/**
 * Reads the ProblemDetails `detail` string from an Axios error response, when
 * present. The backend returns the raw business message there for 409s.
 */
const extractDetail = (error: unknown): string | undefined => {
  if (!(error instanceof AxiosError)) {
    return undefined;
  }

  const data = error.response?.data as { detail?: unknown } | undefined;
  return typeof data?.detail === 'string' ? data.detail : undefined;
};

/**
 * Maps a failed roster-registration error into a clear Spanish message,
 * distinguishing the three 409 conflicts by matching the backend detail text
 * (duplicate dorsal / roster full / player already in another team). Any other
 * error falls back to a generic message.
 *
 * @param error The error thrown by the registration request.
 * @returns The user-facing message to surface.
 */
export const mapRosterConflictMessage = (error: unknown): string => {
  const status =
    error instanceof AxiosError ? error.response?.status : undefined;

  if (status !== HttpStatus.Conflict) {
    return ROSTER_CONFLICT_MESSAGES.generic;
  }

  const detail = (extractDetail(error) ?? '').toLowerCase();

  if (detail.includes('jersey number')) {
    return ROSTER_CONFLICT_MESSAGES.duplicateDorsal;
  }

  if (detail.includes('maximum of')) {
    return ROSTER_CONFLICT_MESSAGES.rosterFull;
  }

  if (detail.includes('another team') || detail.includes('two teams')) {
    return ROSTER_CONFLICT_MESSAGES.alreadyInAnotherTeam;
  }

  return ROSTER_CONFLICT_MESSAGES.generic;
};
