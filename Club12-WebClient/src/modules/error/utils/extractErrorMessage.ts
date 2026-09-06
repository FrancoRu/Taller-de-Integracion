import { AxiosError } from 'axios';
import { BadRequestResponse } from '@/modules/error/type/error.d';
import { HttpStatus } from '@/modules/core/constants/httpStatus';
import { ERROR_MESSAGES } from '@/modules/core/constants/constants';

const isBadRequestResponse = (data: unknown): data is BadRequestResponse => {
  return typeof data === 'object' && data !== null && 'title' in data;
};

/**
 * 502/503/504 never come from our own API — they're the reverse proxy
 * (Cloudflare) reporting the backend is unreachable/restarting, typically
 * during a deploy. Its JSON error page also happens to carry a `title`
 * field, which satisfies isBadRequestResponse and would otherwise leak its
 * raw English text (e.g. "Bad Gateway") straight to the user instead of
 * falling back to a Spanish message.
 */
const isGatewayErrorStatus = (status?: number): boolean =>
  status === HttpStatus.BadGateway ||
  status === HttpStatus.ServiceUnavailable ||
  status === HttpStatus.GatewayTimeout;

/**
 * ASP.NET returns two shapes of problem response: a plain ProblemDetails
 * with a `detail` string, or a ValidationProblemDetails with an `errors`
 * dictionary of field -> messages. This flattens either into one
 * human-readable message instead of falling back to the raw axios error
 * text (e.g. "Network Error", "Request failed with status code 400").
 */
const extractMessage = (data: BadRequestResponse): string => {
  if (data.errors) {
    return Object.values(data.errors).flat().join(' ');
  }
  return data.detail ?? data.title ?? ERROR_MESSAGES.GENERIC_ERROR;
};

/**
 * The same message-extraction rules ErrorContext's setError uses to build
 * its toast, exposed standalone for callers that need the actual backend
 * detail in hand — e.g. to show it inside their own persistent dialog,
 * rather than relying on the global toast alone.
 */
export const extractErrorMessage = (error: unknown): string => {
  if (!(error instanceof AxiosError)) {
    return ERROR_MESSAGES.GENERIC_ERROR;
  }

  const data = error.response?.data;

  if (isGatewayErrorStatus(error.response?.status)) {
    return ERROR_MESSAGES.SERVER_UNAVAILABLE;
  }
  if (isBadRequestResponse(data)) {
    return extractMessage(data);
  }
  return error.response ? ERROR_MESSAGES.GENERIC_ERROR : ERROR_MESSAGES.NETWORK_ERROR;
};
