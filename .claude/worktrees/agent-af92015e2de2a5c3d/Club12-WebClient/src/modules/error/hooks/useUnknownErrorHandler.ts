import { AxiosError } from 'axios';
import { useCallback } from 'react';
import { useError } from '@/modules/error/hooks/error.hock';
import { ERROR_MESSAGES } from '@/modules/core/constants/constants';

/**
 * Returns a stable callback that reports an unknown error to the global
 * error context, wrapping non-Axios errors in a generic AxiosError so the
 * rest of the app only ever has to handle one error shape.
 */
export const useUnknownErrorHandler = () => {
  const { setError } = useError();

  return useCallback(
    (error: unknown) => {
      if (error instanceof AxiosError) {
        setError(error);
        return;
      }

      setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
    },
    [setError]
  );
};
