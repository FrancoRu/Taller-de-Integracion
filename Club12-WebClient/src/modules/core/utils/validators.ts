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

/** A player's DNI/document number: digits only, 6 to 15 of them. */
const DOCUMENT_NUMBER_REGEX = /^\d{6,15}$/;

/** Minimum age (years) a player must be, mirroring the backend's [MinimumAge(15)]. */
const PLAYER_MINIMUM_AGE = 15;

/** True when `value` is a syntactically valid email address. */
export function isValidEmail(value: string): boolean {
  return EMAIL_REGEX.test(value.trim());
}

/**
 * True when `value` is a plausible Argentine phone number: only digits,
 * spaces, `+`, `-`, and parentheses, and a digit count/prefix combination
 * that matches how Argentine numbers are actually written:
 * - 10 digits: a bare local number (area code + line, no prefix).
 * - 11 digits: a mobile marked with a leading `9`, or a local number with
 *   the domestic long-distance `0` trunk prefix (e.g. "011 4567-8901").
 * - 12 digits: the `54` country code plus a landline (no mobile marker).
 * - 13 digits: the `549` country code plus the mobile marker.
 */
export function isValidPhone(value: string): boolean {
  const trimmed = value.trim();

  if (!PHONE_ALLOWED_CHARS_REGEX.test(trimmed)) {
    return false;
  }

  const digits = trimmed.replace(/\D/g, '');
  switch (digits.length) {
    case 10:
      return true;
    case 11:
      return digits.startsWith('9') || digits.startsWith('0');
    case 12:
      return digits.startsWith('54') && !digits.startsWith('549');
    case 13:
      return digits.startsWith('549');
    default:
      return false;
  }
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
 * Formats a phone number as an Argentine mobile number for display, e.g.
 * "3435551234" → "+54 9 343 555-1234". Accepts the number with or without
 * the "+54"/"9" prefixes already present (idempotent either way). Only a
 * 10-digit local number (area code + line, the shape every phone in this
 * app is stored as) can be confidently split into area/exchange/line
 * without an area-code length table, so anything else is returned
 * unchanged rather than mangled.
 */
export function formatArgentinePhone(value: string): string {
  const digits = value.replace(/\D/g, '');
  let local = digits;
  if (local.startsWith('54')) {
    local = local.slice(2);
  }
  if (local.startsWith('9')) {
    local = local.slice(1);
  }

  if (local.length !== 10) {
    return value;
  }

  const area = local.slice(0, 3);
  const exchange = local.slice(3, 6);
  const line = local.slice(6);
  return `+54 9 ${area} ${exchange}-${line}`;
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
