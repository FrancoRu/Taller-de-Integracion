import { useContext } from 'react';
import { AuthContext } from '@/modules/auth/context/auth.context';

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used whithin an Auth Provider');
  }
  return context;
};
