import { GUID } from '@/modules/core/types/types';
import { PlayerFiltered } from '@/modules/player/type/player.d';
import {
  isAtLeastMinimumPlayerAge,
  isValidDocumentNumber,
  isValidPhone,
  VALIDATION_MESSAGES,
} from '@/modules/core/utils/validators';

export type PlayersSearchFilters = Pick<
  PlayerFiltered,
  'names' | 'lastName' | 'documentNumber' | 'phoneNumber'
>;

export type PlayerFormState = {
  firstName: string;
  secondName: string;
  lastName: string;
  documentNumber: string;
  birthDate: string;
  phoneNumber: string;
  socialSecurity: string;
  teamId: GUID | '';
  /** Dorsal for the current team/tournament roster (HU-54), editable only
   * when a roster context is in scope — a dorsal is season-scoped, so it's
   * meaningless outside a team+tournament context. */
  jerseyNumber: string;
};

/** The plain text/select fields of {@link PlayerFormState} (excludes the
 * team select and the roster-scoped dorsal, which have their own dedicated
 * handling in {@link PlayerFormFields}). */
export type PlayerFormField =
  | 'firstName'
  | 'secondName'
  | 'lastName'
  | 'documentNumber'
  | 'birthDate'
  | 'phoneNumber'
  | 'socialSecurity';

/** Formats a `Date` (or date-ish value) into the `yyyy-MM-dd` string an
 * `<input type="date">` expects. Returns an empty string for an invalid or
 * missing value. */
export const toDateInputValue = (value?: string | Date | null): string => {
  if (!value) {
    return '';
  }

  const dateValue = new Date(value);
  if (Number.isNaN(dateValue.getTime())) {
    return '';
  }

  return dateValue.toISOString().slice(0, 10);
};

/** The identity fields required to create a player, regardless of whether
 * they came from the inline roster table or a CSV import row. */
export interface PlayerFieldsForValidation {
  firstName: string;
  secondName?: string;
  lastName: string;
  documentNumber: string;
  /** A 'yyyy-MM-dd' string, matching an `<input type="date">`'s value. */
  birthDate: string;
  phoneNumber: string;
  socialSecurity: string;
}

/**
 * The single source of truth for "is this enough to create a player" —
 * shared by the roster table's inline add row and the CSV batch import, so
 * both flows reject the same bad data with the same Spanish message.
 */
export const validatePlayerFields = (
  fields: PlayerFieldsForValidation,
  resolvedTeamId: GUID | ''
): { title: string; text: string } | null => {
  if (
    !fields.firstName.trim() ||
    !fields.lastName.trim() ||
    !fields.documentNumber.trim() ||
    !fields.birthDate.trim() ||
    !fields.phoneNumber.trim() ||
    !fields.socialSecurity.trim()
  ) {
    return {
      title: 'Campos incompletos',
      text: 'Nombre, apellido, documento, fecha de nacimiento, teléfono y seguro social son obligatorios. El segundo nombre es opcional.',
    };
  }

  if (!isValidPhone(fields.phoneNumber)) {
    return { title: 'Teléfono inválido', text: `${VALIDATION_MESSAGES.phone}.` };
  }

  if (!isValidDocumentNumber(fields.documentNumber)) {
    return {
      title: 'Documento inválido',
      text: `${VALIDATION_MESSAGES.documentNumber}.`,
    };
  }

  if (!isAtLeastMinimumPlayerAge(fields.birthDate)) {
    return {
      title: 'Fecha de nacimiento inválida',
      text: `${VALIDATION_MESSAGES.minimumPlayerAge}.`,
    };
  }

  if (!resolvedTeamId) {
    return { title: 'Equipo requerido', text: 'Debe seleccionar un equipo.' };
  }

  return null;
};
