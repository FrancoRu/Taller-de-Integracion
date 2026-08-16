import { createContext, useEffect, useState } from 'react';
import { AxiosError } from 'axios';
import Swal from 'sweetalert2';
import { ProviderProps } from '@/modules/core/types/types';
import { BadRequestResponse, IErrorContextProp } from '@/modules/error/type/error.d';
import { HttpStatus } from '@/modules/core/constants/httpStatus';

export const ErrorContext = createContext<IErrorContextProp | undefined>(
  undefined
);

export const ErrorProvider: React.FC<ProviderProps> = ({ children }) => {
  const [errors, setErrors] = useState<string[]>([]);

  const isBadRequestResponse = (data: unknown): data is BadRequestResponse => {
    return (
      typeof data === 'object' &&
      data !== null &&
      'detail' in data &&
      'title' in data &&
      'status' in data
    );
  };

  /**
   * Only adds the new error message if it isn't already present, to avoid
   * showing duplicate error messages for the same underlying failure.
   */
  const setError = (error: AxiosError) => {
    const data = error.response?.data;
    if (isBadRequestResponse(data)) {
      const message = data.detail ?? 'Error in the request';
      const status =
        data.statusCode ?? error.response?.status ?? HttpStatus.BadRequest;

      setErrors(prevErrors => {
        if (!prevErrors.includes(message)) {
          return [...prevErrors, message];
        }
        return prevErrors;
      });
      setMessage(status, [message]);
    } else {
      const fallbackMessage = error.message || 'Unknown error';
      setErrors(prevErrors => {
        if (!prevErrors.includes(fallbackMessage)) {
          return [...prevErrors, fallbackMessage];
        }
        return prevErrors;
      });
      setMessage(error.response?.status ?? HttpStatus.InternalServerError, [
        fallbackMessage,
      ]);
    }
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
      color: 'black',
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
