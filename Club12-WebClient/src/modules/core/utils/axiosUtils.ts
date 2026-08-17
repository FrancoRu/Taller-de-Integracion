import axios, { AxiosError, AxiosResponse } from 'axios';
import jsCookie from 'js-cookie';
import routes from '@/modules/core/constants/routes';
import {
  ERROR_MESSAGES,
  COOKIE_SIGNIN_TOKEN,
  JWT,
} from '@/modules/core/constants/constants';
import { HttpStatus } from '@/modules/core/constants/httpStatus';

const TOKEN_KEY: string = COOKIE_SIGNIN_TOKEN;
const INVALID_TOKEN_PATH = routes.tokenInvalido;

type headersContent = {
  'Content-Type'?: string;
  Authorization?: string;
};

type ConfigOverride = {
  headers?: headersContent;
};

const statusCodeHandlers: Record<
  number,
  ((response: AxiosResponse) => void)[]
> = {};

const hasAuthorizationHeader = (error: AxiosError): boolean => {
  const headers = error.config?.headers as Record<string, unknown> | undefined;

  const authorizationHeader = headers?.Authorization ?? headers?.authorization;
  return Boolean(authorizationHeader);
};

const isRefreshTokenRequest = (error: AxiosError): boolean => {
  const requestUrl = error.config?.url ?? '';
  return requestUrl.includes('/auth/refresh-token');
};

const redirectToInvalidToken = (): void => {
  if (typeof window === 'undefined') {
    return;
  }

  if (window.location.pathname === INVALID_TOKEN_PATH) {
    return;
  }

  unregisterToken();
  localStorage.removeItem(JWT.REFRESH_TOKEN);
  window.location.assign(INVALID_TOKEN_PATH);
};

const triggerStatusCodeHandlers = (response: AxiosResponse): void => {
  const handlers = statusCodeHandlers[response.status];
  if (!handlers?.length) {
    return;
  }

  handlers.forEach(callback => {
    callback(response);
  });
};

const handleUnauthorizedToken = (error: AxiosError): void => {
  const statusCode = error.response?.status;
  if (statusCode !== HttpStatus.Unauthorized) {
    return;
  }

  const shouldRedirect =
    hasAuthorizationHeader(error) || isRefreshTokenRequest(error);

  if (shouldRedirect) {
    redirectToInvalidToken();
  }
};

/**
 * Checks if a token is set in cookies.
 * @returns {boolean} True if the token exists, false otherwise.
 */
export const tokenIsSet = (): boolean => !!jsCookie.get(TOKEN_KEY);

/**
 * Registers a token in cookies.
 * @param {string} newToken - The token to store.
 * @param {Date} expirationDate - The expiration date for the token.
 */
export const registerToken = (newToken: string, expirationDate: Date) => {
  jsCookie.set(TOKEN_KEY, newToken, {
    expires: expirationDate,
    sameSite: 'lax',
    path: '/',
  });
};

/**
 * Unregisters (removes) the token from cookies.
 */
export const unregisterToken = (): void => {
  jsCookie.remove(TOKEN_KEY, {
    path: '/',
  });
};

/**
 * Retrieves the currently registered token.
 * @returns {string | undefined} The registered token, or undefined if none is set.
 */
export const getRegisteredToken = (): string | undefined =>
  jsCookie.get(TOKEN_KEY);

/**
 * Retrieves the default headers for requests.
 * @returns {headersContent} The default headers.
 */
const getDefaultHeaders = (): headersContent => {
  const headers: headersContent = {
    'Content-Type': 'application/json; charset=utf-8',
  };

  if (tokenIsSet()) {
    const token = jsCookie.get(TOKEN_KEY);
    headers.Authorization = `Bearer ${token}`;
  }
  return headers;
};

/**
 * Merges custom headers with default headers.
 * @param {ConfigOverride} [configOverride] - The override configuration.
 * @returns {headersContent} The resulting headers.
 */
const getHeaders = (configOverride?: ConfigOverride): headersContent => {
  let headers: headersContent = getDefaultHeaders();

  if (configOverride?.headers) {
    headers = {
      ...headers,
      ...configOverride.headers,
    };
  }

  return headers;
};

/**
 * Builds the full API endpoint URL, including optional query parameters.
 * @param {string} resource - The API resource.
 * @param {object} [query] - The query parameters as an object.
 * @returns {string} The encoded full endpoint URL.
 */
export const buildEndpoint = (resource: string, query?: object): string => {
  const finalResource = `${routes.apiUrl}/${resource}`;
  if (query) {
    const queryParams = Object.entries(query)
      .filter(
        ([, value]) => value !== undefined && value !== null && value !== ''
      )
      .map(
        ([key, value]) =>
          `${encodeURIComponent(key)}=${encodeURIComponent(String(value))}`
      )
      .join('&');

    if (queryParams.length > 0) {
      return `${finalResource}?${queryParams}`;
    }
  }

  return finalResource;
};

/**
 * Sends an HTTP request.
 * @param {string} method - HTTP method (GET, POST, PUT, DELETE).
 * @param {string} resource - API resource.
 * @param {object} [configOverride] - Request configuration overrides.
 * @param {unknown | null} body - Request body data.
 * @param {object} [query] - Query parameters.
 * @returns {Promise<AxiosResponse<T>>} A promise that resolves with the response or undefined.
 */
const sendRequest = async <T>(
  method: string,
  resource: string,
  configOverride: object = {},
  body: unknown | null = null,
  query?: object
): Promise<AxiosResponse<T>> => {
  const headers = getHeaders(configOverride);
  if (body instanceof FormData) {
    delete headers['Content-Type'];
  }
  const url = buildEndpoint(resource, query);
  try {
    const result: AxiosResponse<T> = await axios.request({
      method,
      url,
      headers,
      data: body,
    });
    return result;
  } catch (error: unknown) {
    throw throwError(error);
  }
};

/**
 * Throws an appropriate error based on its type.
 * @param {unknown} error - The error object.
 * @returns {AxiosError | Error} - The error object processed.
 */
const throwError = (error: unknown): AxiosError | Error => {
  switch (true) {
    case axios.isAxiosError(error): {
      if (error.response) {
        triggerStatusCodeHandlers(error.response);
      }
      handleUnauthorizedToken(error);
      return error;
    }

    case error instanceof Error:
      return new AxiosError(
        error.message,
        undefined,
        undefined,
        undefined,
        undefined
      );

    default:
      return new AxiosError(ERROR_MESSAGES.GENERIC_ERROR);
  }
};

/**
 * Sends a POST HTTP request.
 * @param {string} resource - API resource.
 * @param {unknown} [body] - Request body.
 * @param {ConfigOverride} [configOverride] - Configuration overrides.
 * @returns {Promise<AxiosResponse<T>>} A promise that resolves with the server response.
 */
export const sendPost = async <T>(
  resource: string,
  body?: unknown,
  configOverride?: ConfigOverride
): Promise<AxiosResponse<T>> => {
  return await sendRequest<T>('POST', resource, configOverride, body);
};

/**
 * Sends a PUT HTTP request.
 * @param {string} resource - API resource.
 * @param {unknown} body - Request body.
 * @param {ConfigOverride} [configOverride] - Configuration overrides.
 * @returns {Promise<AxiosResponse<T>>} A promise that resolves with the server response.
 */
export const sendPut = async <T>(
  resource: string,
  body: unknown,
  configOverride?: ConfigOverride
): Promise<AxiosResponse<T>> => {
  return await sendRequest<T>('PUT', resource, configOverride, body);
};

/**
 * Sends a GET HTTP request.
 * @param {string} resource - API resource.
 * @param {object} [query] - Query parameters.
 * @returns {Promise<AxiosResponse<T>>} A promise that resolves with the server response.
 */
export const sendGet = async <T>(
  resource: string,
  query?: object
): Promise<AxiosResponse<T>> => {
  return await sendRequest<T>('GET', resource, {}, null, query);
};

/**
 * Sends a DELETE HTTP request.
 * @param {string} resource - API resource.
 * @param {ConfigOverride} [configOverride] - Configuration overrides.
 * @returns {Promise<AxiosResponse<T>>} A promise that resolves when the resource is deleted.
 */
export const sendDelete = async <T>(
  resource: string,
  configOverride?: ConfigOverride
): Promise<AxiosResponse<T>> =>
  await sendRequest<T>('DELETE', resource, configOverride);

/**
 * Downloads a file from the server.
 * @param {string} resource - API resource.
 * @param {string} fileNameWithExtension - Name of the file to save locally.
 */
export const downloadfile = async (
  resource: string,
  fileNameWithExtension: string
) => {
  const headers = getHeaders();
  const url = buildEndpoint(resource);

  const result = await axios.get(url, { headers, responseType: 'blob' });

  const fileUrl = window.URL.createObjectURL(new Blob([result.data]));
  const link = document.createElement('a');
  link.href = fileUrl;
  link.setAttribute('download', fileNameWithExtension);
  document.body.appendChild(link);
  link.click();
  link.remove();
};

/**
 * Registers a callback for a specific HTTP status code.
 * @param {number} statusCode - HTTP status code.
 * @param {() => unknown} callback - Function to execute when the status code is received.
 */
export const onStatusCode = (statusCode: number, callback: () => unknown) => {
  if (statusCodeHandlers[statusCode]) {
    statusCodeHandlers[statusCode].push(callback);
  } else {
    statusCodeHandlers[statusCode] = [callback];
  }
};

/**
 * Registers a callback for the 401 Unauthorized status code.
 * @param {() => unknown} callback - Function to execute when a 401 status code is received.
 */
export const onUnauthorized = (callback: () => unknown) => {
  onStatusCode(HttpStatus.Unauthorized, callback);
};
