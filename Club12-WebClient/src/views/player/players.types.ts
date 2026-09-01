import { GUID } from '@/modules/core/types/types';
import { PlayerFiltered } from '@/modules/player/type/player.d';

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
