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
import { extractErrorMessage } from '@/modules/error/utils/extractErrorMessage';
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
      const message = extractErrorMessage(error);
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
