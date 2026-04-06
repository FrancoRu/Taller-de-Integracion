// src/error/context/error.context.tsx

import { createContext, useEffect, useState } from 'react';
import { AxiosError } from 'axios';
import Swal from 'sweetalert2';
import { ProviderProps } from '@/modules/core/types/types';
import { BadRequestResponse, IErrorContextProp } from '@/modules/error/type/error.d';

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

  const setError = (error: AxiosError) => {
    const data = error.response?.data;
    if (isBadRequestResponse(data)) {
      const message = data.detail ?? 'Error in the request';
      const status = data.statusCode ?? error.response?.status ?? 400;

      // Only add new errors if they're not already present to avoid duplicates
      // Or if you want to show all errors, just use: setErrors(prevErrors => [...prevErrors, message]);
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
      setMessage(error.response?.status ?? 500, [fallbackMessage]);
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
  }, [errors]); // Dependency: `errors` state

  return (
    <ErrorContext.Provider value={{ errors, setError, setMessage }}>
      {children}
    </ErrorContext.Provider>
  );
};
