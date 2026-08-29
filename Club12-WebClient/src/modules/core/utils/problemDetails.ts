import { AxiosError } from 'axios';

/**
 * Discriminated outcome of a mutation that can fail with a user-facing reason
 * (e.g. a 409 Conflict the backend returns with a Spanish message). Callers
 * surface {@link errorMessage} inline instead of routing the failure through
 * the global error handler.
 */
export type MutationResult =
  | { success: true }
  | { success: false; errorMessage: string };

/**
 * Reads the ProblemDetails `detail` string from an Axios error response, when
 * present. The backend returns the raw business message there for 4xx errors.
 *
 * @param error The error thrown by a request.
 * @returns The `detail` message, or undefined when it is not an Axios error
 * with a string detail.
 */
export const extractProblemDetail = (error: unknown): string | undefined => {
  if (!(error instanceof AxiosError)) {
    return undefined;
  }

  const data = error.response?.data as { detail?: unknown } | undefined;
  return typeof data?.detail === 'string' ? data.detail : undefined;
};
