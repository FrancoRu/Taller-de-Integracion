/**
 * Shared email / phone validators reused by every form that collects contact
 * data (user create/invite/edit, player create/edit, password recovery).
 *
 * The rules intentionally mirror the backend DataAnnotations in
 * `Application.Utils.Constants.Validation.ValidationPatterns`, so the client and
 * the server accept exactly the same values and the user never gets a 400 for
 * input the form said was fine.
 */

/** Standard, permissive email shape: local@domain.tld with no whitespace. */
const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/** Characters allowed in a plausible phone number. */
const PHONE_ALLOWED_CHARS_REGEX = /^[+\d\s()-]+$/;

const PHONE_MIN_DIGITS = 8;
const PHONE_MAX_DIGITS = 15;

/** A player's DNI/document number: digits only, 6 to 15 of them. */
const DOCUMENT_NUMBER_REGEX = /^\d{6,15}$/;

/** Minimum age (years) a player must be, mirroring the backend's [MinimumAge(15)]. */
const PLAYER_MINIMUM_AGE = 15;

/** True when `value` is a syntactically valid email address. */
export function isValidEmail(value: string): boolean {
  return EMAIL_REGEX.test(value.trim());
}

/**
 * True when `value` is a plausible phone number: only digits, spaces, `+`,
 * `-`, and parentheses, containing between 8 and 15 digits.
 */
export function isValidPhone(value: string): boolean {
  const trimmed = value.trim();

  if (!PHONE_ALLOWED_CHARS_REGEX.test(trimmed)) {
    return false;
  }

  const digitCount = (trimmed.match(/\d/g) ?? []).length;
  return digitCount >= PHONE_MIN_DIGITS && digitCount <= PHONE_MAX_DIGITS;
}

/** True when `value` is a plausible DNI/document number: 6 to 15 digits only. */
export function isValidDocumentNumber(value: string): boolean {
  return DOCUMENT_NUMBER_REGEX.test(value.trim());
}

/**
 * True when `birthDate` (an `<input type="date">` value, "YYYY-MM-DD") puts
 * the person at least {@link PLAYER_MINIMUM_AGE} years old today. Parses the
 * Y/M/D components explicitly and builds a LOCAL date rather than relying on
 * `new Date("YYYY-MM-DD")` — that form is UTC-midnight per spec, which would
 * silently shift the effective date by a day in any timezone behind UTC (all
 * of Argentina), misjudging someone born exactly on the cutoff date. An
 * unparsable value is treated as invalid — the caller already requires the
 * field, so an empty/malformed string should never reach here as "valid".
 */
export function isAtLeastMinimumPlayerAge(birthDate: string): boolean {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(birthDate.trim());
  if (!match) {
    return false;
  }

  const [, year, month, day] = match;
  const parsed = new Date(Number(year), Number(month) - 1, Number(day));
  if (Number.isNaN(parsed.getTime())) {
    return false;
  }

  const cutoff = new Date();
  cutoff.setHours(0, 0, 0, 0);
  cutoff.setFullYear(cutoff.getFullYear() - PLAYER_MINIMUM_AGE);

  return parsed.getTime() <= cutoff.getTime();
}

/**
 * Formats a DNI/document number with dot thousands-separators for display
 * (e.g. "38742615" → "38.742.615", matching the printed-DNI convention).
 * Non-numeric input (legacy/test data) is returned unchanged rather than
 * mangled.
 */
export function formatDocumentNumber(value: string): string {
  if (!isValidDocumentNumber(value)) {
    return value;
  }

  return Number(value).toLocaleString('es-AR');
}

/** Spanish (voseo) helper/error messages shown under the fields. */
export const VALIDATION_MESSAGES = {
  email: 'Ingresá un email válido',
  phone: 'Ingresá un teléfono válido',
  documentNumber: 'El documento debe tener solo números',
  minimumPlayerAge: `El jugador debe tener al menos ${PLAYER_MINIMUM_AGE} años`,
} as const;
