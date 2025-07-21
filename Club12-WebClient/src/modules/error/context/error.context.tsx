import { createContext, useEffect, useState } from 'react';
import { AxiosError } from 'axios';
import Swal from 'sweetalert2';
import { ProviderProps } from '../../core/types/types';
import { IErrorContextProp } from '../type/error.d';

export const ErrorContext = createContext<IErrorContextProp | undefined>(
  undefined
);

export const ErrorProvider: React.FC<ProviderProps> = ({ children }) => {
  const [errors, setErrors] = useState<string[]>([]);

  const setError = (error: AxiosError) => {
    const axiosError = error.message as unknown as AxiosError;
    const data = axiosError.response?.data ?? 'Error in the request';
    const status = axiosError.response?.status ?? 404;

    setErrors(prevErrors => [...prevErrors, data as string]);
    setMessage(status, [data as string]);
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
    if (errors !== null) {
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
