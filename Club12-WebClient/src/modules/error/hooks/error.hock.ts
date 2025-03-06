import { useContext } from 'react';
import { ErrorContext } from '../context/error.context';

export const useError = () => {
  const context = useContext(ErrorContext);
  if (!context) {
    throw new Error('useError must be used whithin an Error Provider');
  }
  return context;
};
