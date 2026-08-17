export const COOKIE_SIGNIN_TOKEN = 'Club12_SignInToken';

export const SUCCESS_MESSAGES = {
  LOGIN_SUCCESS: 'Sesión iniciada correctamente',
};

export const ERROR_MESSAGES = {
  GENERIC_ERROR: 'Ocurrió un error. Por favor, intentá nuevamente.',
  NETWORK_ERROR:
    'No se pudo conectar con el servidor. Verificá tu conexión e intentá nuevamente.',
};

export const EXPIRATION_TIME = {
  MS_IN_HOUR: 3600 * 1000,
  MS_IN_MINUTE: 60 * 1000,
  MS_IN_SECOND: 1000,
};

export const JWT = {
  ACCESS_TOKEN: 'accessToken',
  REFRESH_TOKEN: 'refreshToken',
  EXPIRES_IN: 'expiresIn',
};

export const FILTERS_DEBOUNCE_DELAY_MS = 500;
export const FILTERS_DEBOUNCE_DELAY_LONG_MS = 1000;
export const PUBLIC_SEARCH_DEBOUNCE_DELAY_MS = 600;

/**
 * Applied to tabbed content areas (tournament/division tabs) so switching
 * between a short tab (e.g. Información) and a long one (e.g. Partidos)
 * doesn't visibly jump the page height and the footer position with it.
 */
export const TAB_CONTENT_MIN_HEIGHT = 400;

export const USERNAME_LENGTH = {
  Min: 3,
  Max: 50,
} as const;
