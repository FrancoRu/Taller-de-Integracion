import {
  createContext,
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react';
import { AxiosError } from 'axios';
import Swal from 'sweetalert2';
import { ProviderProps } from '@/modules/core/types/types';
import { BadRequestResponse, IErrorContextProp } from '@/modules/error/type/error.d';
import { HttpStatus } from '@/modules/core/constants/httpStatus';
import { ERROR_MESSAGES } from '@/modules/core/constants/constants';
import { getTheme } from '@/theme';

const theme = getTheme('dark');
const DIALOG_BACKGROUND = theme.palette.background.paper;
const DIALOG_TEXT_COLOR = theme.palette.text.primary;

export const ErrorContext = createContext<IErrorContextProp | undefined>(
  undefined
);

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

export const ErrorProvider: React.FC<ProviderProps> = ({ children }) => {
  const [errors, setErrors] = useState<string[]>([]);
  // Remembers the last toast so an identical message fired again within a short
  // window (e.g. two chained requests that both succeed, or the same error
  // surfaced by both an interceptor and a catch) shows only ONCE instead of
  // stacking repeated alerts.
  const lastToastRef = useRef<{ message: string; at: number }>({
    message: '',
    at: 0,
  });

  const setMessage = useCallback((status: number, message: string[]) => {
    const stat = status < 400 ? 'success' : 'error';
    const messages = message.join(', ');

    const now = Date.now();
    if (
      lastToastRef.current.message === messages &&
      now - lastToastRef.current.at < 2500
    ) {
      return;
    }
    lastToastRef.current = { message: messages, at: now };

    void Swal.fire({
      position: 'center',
      icon: stat,
      title: messages,
      showConfirmButton: false,
      timer: 1500,
      background: DIALOG_BACKGROUND,
      color: DIALOG_TEXT_COLOR,
      // Keep toasts above MUI's modal layer so one fired while a Dialog is open
      // is not hidden behind it.
      didOpen: () => {
        const container = Swal.getContainer();
        if (container) {
          container.style.zIndex = '2000';
        }
      },
    });
  }, []);

  /**
   * Only adds the new error message if it isn't already present, to avoid
   * showing duplicate error messages for the same underlying failure.
   */
  const setError = useCallback(
    (error: AxiosError) => {
      const data = error.response?.data;
      const message = isGatewayErrorStatus(error.response?.status)
        ? ERROR_MESSAGES.SERVER_UNAVAILABLE
        : isBadRequestResponse(data)
          ? extractMessage(data)
          : error.response
            ? ERROR_MESSAGES.GENERIC_ERROR
            : ERROR_MESSAGES.NETWORK_ERROR;
      const status =
        (isBadRequestResponse(data) ? (data.statusCode ?? data.status) : undefined) ??
        error.response?.status ??
        HttpStatus.InternalServerError;

      setErrors(prevErrors =>
        prevErrors.includes(message) ? prevErrors : [...prevErrors, message]
      );
      setMessage(status, [message]);
    },
    [setMessage]
  );

  useEffect(() => {
    if (errors.length > 0) {
      const timer = setTimeout(() => {
        setErrors([]);
      }, 5000);
      return () => clearTimeout(timer);
    }
  }, [errors]);

  const value = useMemo(
    () => ({ errors, setError, setMessage }),
    [errors, setError, setMessage]
  );

  return (
    <ErrorContext.Provider value={value}>{children}</ErrorContext.Provider>
  );
};
