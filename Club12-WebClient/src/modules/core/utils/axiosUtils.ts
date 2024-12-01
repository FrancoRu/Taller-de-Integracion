import axios, { AxiosError, AxiosResponse } from 'axios';
import jsCookie from 'js-cookie';
import routes from '../constants/routes';
import { COOKIE_SIGNIN_TOKEN } from '../constants/constants';

const TOKEN_KEY: string = COOKIE_SIGNIN_TOKEN;

type headersContent = {
  'Content-Type': string;
  Authorization?: string;
};

type ConfigOverride = {
  headers?: headersContent;
};

const statusCodeHandlers: Record<
  number,
  ((response: AxiosResponse) => void)[]
> = {};

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
  let finalResource = `${routes.apiUrl}/${resource}`;

  if (query) {
    const queryParams = Object.entries(query)
      .map(
        ([key, value]) =>
          `${encodeURIComponent(key)}=${encodeURIComponent(value)}`
      )
      .join('&');
    finalResource += `?${queryParams}`;
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
  const url = buildEndpoint(resource, query); // Pass query to buildEndpoint
  try {
    const result: AxiosResponse<T> = await axios.request({
      method,
      url,
      headers,
      data: body,
    });
    return result;
  } catch (error: unknown) {
    throwError(error);
    throw new Error('Unexpected error during request execution');
  }
};

/**
 * Throws an appropriate error based on its type.
 * @param {unknown} error - The error object.
 */
const throwError = (error: unknown) => {
  switch (true) {
    case axios.isAxiosError(error):
      throw error;

    case error instanceof Error:
      throw new AxiosError(
        error.message,
        undefined,
        undefined,
        undefined,
        undefined
      );

    default:
      throw new AxiosError('An unknown error occurred');
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
  try {
    const result: AxiosResponse<T> = await axios.request({
      method: 'GET',
      url: buildEndpoint(resource, query),
      headers: getHeaders(),
    });
    return result;
  } catch (error) {
    console.error('Error in sendGet:', error); // Log any errors
    throw error;
  }
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
): Promise<AxiosResponse<T>> => {
  return await sendRequest<T>('DELETE', resource, configOverride);
};

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
  onStatusCode(401, callback);
};
