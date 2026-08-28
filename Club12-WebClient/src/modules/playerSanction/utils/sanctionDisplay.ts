import {
  IPlayerSanctionResponse,
  SanctionSubjectType,
} from '@/modules/playerSanction/type/playerSanction.d';

/** Spanish label for each sanction subject kind (HU-77). */
export const SANCTION_SUBJECT_TYPE_LABEL: Record<SanctionSubjectType, string> = {
  Player: 'Jugador',
  Team: 'Equipo',
  Staff: 'Staff',
};

/**
 * Sanctions recorded as a permanent/indefinite ban (e.g. expulsion) are stored
 * with an arbitrarily large duration rather than a real fecha count — show it
 * as "Permanente" instead of a literal, meaningless number.
 */
export const PERMANENT_SANCTION_DURATION_THRESHOLD = 999;

type SanctionSubject = Pick<
  IPlayerSanctionResponse,
  'subjectType' | 'playerFullName' | 'teamName' | 'staffName'
>;

const EMPTY_VALUE = '—';

/**
 * The human-readable subject of a sanction, resolved by its subjectType
 * (HU-77): a player's full name, a team's name, or a staff member's name.
 */
export const getSanctionSubjectName = (sanction: SanctionSubject): string => {
  switch (sanction.subjectType) {
    case 'Team':
      return sanction.teamName || EMPTY_VALUE;
    case 'Staff':
      return sanction.staffName || EMPTY_VALUE;
    case 'Player':
    default:
      return sanction.playerFullName || EMPTY_VALUE;
  }
};

/** The Spanish subject-kind label of a sanction (HU-77). */
export const getSanctionSubjectTypeLabel = (
  sanction: Pick<IPlayerSanctionResponse, 'subjectType'>
): string => SANCTION_SUBJECT_TYPE_LABEL[sanction.subjectType] ?? EMPTY_VALUE;

/**
 * Formats a sanction's duration expressed in FECHAS (jornadas, HU-75) — never
 * in calendar days. Shows "Permanente" for indefinite bans and pluralizes the
 * "fecha"/"fechas" unit.
 */
export const formatSanctionDurationFechas = (duration: number): string => {
  if (duration >= PERMANENT_SANCTION_DURATION_THRESHOLD) {
    return 'Permanente';
  }

  return `${duration} ${duration === 1 ? 'fecha' : 'fechas'}`;
};

/**
 * Formats the fechas still to be served (HU-75). Null (not computable) renders
 * as an em dash.
 */
export const formatFechasRemaining = (
  fechasRemaining?: number | null
): string =>
  fechasRemaining === null || fechasRemaining === undefined
    ? EMPTY_VALUE
    : `${fechasRemaining} ${fechasRemaining === 1 ? 'fecha' : 'fechas'}`;

/** The active/served state label for a sanction (HU-75/HU-76). */
export const getSanctionStateLabel = (
  sanction: Pick<IPlayerSanctionResponse, 'isActive'>
): string => (sanction.isActive ? 'Activa' : 'Cumplida');
