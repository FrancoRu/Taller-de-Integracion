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

/**
 * ASP.NET Core Identity's default `IdentityOptions.User.AllowedUserNameCharacters`
 * (letters, digits, and `-._@+`) — mirrored here so an invalid username (most
 * commonly one with spaces, since the field's Spanish label "Nombre de
 * usuario" reads like it could take a full name) is caught client-side with a
 * Spanish message, instead of round-tripping to the backend and surfacing its
 * raw English Identity error ("Username '...' is invalid, can only contain
 * letters or digits.") straight to the admin.
 */
const USERNAME_REGEX = /^[a-zA-Z0-9\-._@+]+$/;

/** Minimum age (years) a player must be, mirroring the backend's [MinimumAge(15)]. */
const PLAYER_MINIMUM_AGE = 15;

/** True when `value` is a syntactically valid email address. */
export function isValidEmail(value: string): boolean {
  return EMAIL_REGEX.test(value.trim());
}

/**
 * True when `value` is a plausible Argentine phone number: only digits,
 * spaces, `+`, `-`, and parentheses, and exactly 10 digits — the national
 * format (area code + local number) used for calls placed from inside the
 * country, with no leading `0` trunk prefix, no `15`, no `+54` country code
 * and no `9` mobile marker (those only apply to international dialing,
 * which this app — a local league — never needs).
 */
export function isValidPhone(value: string): boolean {
  const trimmed = value.trim();

  if (!PHONE_ALLOWED_CHARS_REGEX.test(trimmed)) {
    return false;
  }

  const digits = trimmed.replace(/\D/g, '');
  return digits.length === 10;
}

/** True when `value` is a plausible DNI/document number: 6 to 15 digits only. */
export function isValidDocumentNumber(value: string): boolean {
  return DOCUMENT_NUMBER_REGEX.test(value.trim());
}

/**
 * True when `value` is a valid username: letters, digits, and `-._@+` only
 * (no spaces), matching the backend's actual accepted character set.
 */
export function isValidUsername(value: string): boolean {
  return USERNAME_REGEX.test(value.trim());
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
 * Formats a phone number for display in the Argentine national shape, e.g.
 * "3435551234" → "343 555-1234" — no "+54" country code and no "9" mobile
 * marker, since this app is only ever dialed from inside the country. Only
 * a 10-digit local number (area code + line, the shape every phone in this
 * app is stored as) can be confidently split into area/exchange/line
 * without an area-code length table, so anything else is returned
 * unchanged rather than mangled.
 */
export function formatArgentinePhone(value: string): string {
  const digits = value.replace(/\D/g, '');

  if (digits.length !== 10) {
    return value;
  }

  const area = digits.slice(0, 3);
  const exchange = digits.slice(3, 6);
  const line = digits.slice(6);
  return `${area} ${exchange}-${line}`;
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
  username: 'El nombre de usuario no puede contener espacios ni símbolos (solo letras, números y - . _ @ +)',
} as const;
