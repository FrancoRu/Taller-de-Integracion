import { createContext, useEffect, useState } from 'react';
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

export const ErrorProvider: React.FC<ProviderProps> = ({ children }) => {
  const [errors, setErrors] = useState<string[]>([]);

  const isBadRequestResponse = (data: unknown): data is BadRequestResponse => {
    return typeof data === 'object' && data !== null && 'title' in data;
  };

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
   * Only adds the new error message if it isn't already present, to avoid
   * showing duplicate error messages for the same underlying failure.
   */
  const setError = (error: AxiosError) => {
    const data = error.response?.data;
    const message = isBadRequestResponse(data)
      ? extractMessage(data)
      : error.response
        ? ERROR_MESSAGES.GENERIC_ERROR
        : ERROR_MESSAGES.NETWORK_ERROR;
    const status =
      (isBadRequestResponse(data) ? (data.statusCode ?? data.status) : undefined) ??
      error.response?.status ??
      HttpStatus.InternalServerError;

    setErrors(prevErrors => {
      if (!prevErrors.includes(message)) {
        return [...prevErrors, message];
      }
      return prevErrors;
    });
    setMessage(status, [message]);
  };

  const setMessage = (status: number, message: string[]) => {
    const stat = status < 400 ? 'success' : 'error';
    const messages = message.join(', ');
    Swal.fire({
      position: 'center',
      icon: stat,
      title: messages,
      showConfirmButton: false,
      timer: 1500,
      background: DIALOG_BACKGROUND,
      color: DIALOG_TEXT_COLOR,
    });
  };

  useEffect(() => {
    if (errors.length > 0) {
      const timer = setTimeout(() => {
        setErrors([]);
      }, 5000);
      return () => clearTimeout(timer);
    }
  }, [errors]);

  return (
    <ErrorContext.Provider value={{ errors, setError, setMessage }}>
      {children}
    </ErrorContext.Provider>
  );
};
