import { AxiosError } from 'axios';
import Cookies from 'js-cookie';
import React, { createContext, useState, useEffect } from 'react';
import { ProviderProps } from '../../core/types/types';
import { useError } from '../../error/hooks/error.hock';
import { authService } from '../service/auth.service';
import {
  AuthResponse,
  IAuthContextProps,
  IUser,
  LogInUserRequest,
} from '../type/auth';

import {
  COOKIE_SIGNIN_TOKEN,
  SUCCESS_MESSAGES,
  EXPIRATION_TIME,
} from '../../core/constants/constants';

export const AuthContext = createContext<IAuthContextProps | undefined>(
  undefined
);

export const AuthProvider: React.FC<ProviderProps> = ({ children }) => {
  const [user, setUser] = useState<IUser | null>(null);
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(false);
  const { setError, setMessage } = useError();

  useEffect(() => {
    const token = Cookies.get(COOKIE_SIGNIN_TOKEN);
    if (token) {
      setIsAuthenticated(true);
    }
  }, []);

  const signIn = async (user: LogInUserRequest): Promise<boolean> => {
    try {
      const res = await authService.loginRequest(user);
      if (res?.status === 200 && res?.data) {
        setUser({
          username: user.username,
          accessToken: res?.data as AuthResponse,
        });

        const expiresIn = res.data.expiresIn.split(':').map(Number);
        const expiresInMs =
          expiresIn[0] * EXPIRATION_TIME.MS_IN_HOUR +
          expiresIn[1] * EXPIRATION_TIME.MS_IN_MINUTE +
          expiresIn[2] * EXPIRATION_TIME.MS_IN_SECOND;

        setTimeout(() => {
          Cookies.remove(COOKIE_SIGNIN_TOKEN);
          setIsAuthenticated(false);
        }, expiresInMs);

        Cookies.set(COOKIE_SIGNIN_TOKEN, res.data.accessToken, {
          expires: expiresInMs / (1000 * 60 * 60 * 24),
        });

        setIsAuthenticated(true);
        setMessage(res.status, [SUCCESS_MESSAGES.LOGIN_SUCCESS]);
        return true;
      }
    } catch (error: unknown) {
      setError(error as AxiosError);
      return false;
    }
    return false;
  };

  const logOut = async () => {
    try {
      await authService.logoutRequest();
      Cookies.remove(COOKIE_SIGNIN_TOKEN);
      setIsAuthenticated(false);
      setUser(null);
    } catch (error: unknown) {
      setError(error as AxiosError);
    }
  };

  return (
    <AuthContext.Provider value={{ signIn, logOut, user, isAuthenticated }}>
      {children}
    </AuthContext.Provider>
  );
};
