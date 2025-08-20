import { AxiosError } from 'axios';
import Cookies from 'js-cookie';
import React, {
  createContext,
  useState,
  useEffect,
  useCallback,
  useMemo,
} from 'react'; // Import useCallback and useMemo
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
  }, []); // Empty dependency array means this runs once on mount

  // --- Functions memoized with useCallback ---

  const signIn = useCallback(
    async (userData: LogInUserRequest): Promise<boolean> => {
      // Renamed 'user' to 'userData' for clarity
      try {
        const res = await authService.loginRequest(userData);
        if (res?.status === 200 && res?.data) {
          setUser({
            username: userData.username, // Use userData here
            accessToken: res?.data as AuthResponse,
          });

          // Calculate expiration time in milliseconds
          const expiresIn = res.data.expiresIn.split(':').map(Number);
          const expiresInMs =
            expiresIn[0] * EXPIRATION_TIME.MS_IN_HOUR +
            expiresIn[1] * EXPIRATION_TIME.MS_IN_MINUTE +
            expiresIn[2] * EXPIRATION_TIME.MS_IN_SECOND;

          // Set a timeout to remove the cookie and update auth status
          setTimeout(() => {
            Cookies.remove(COOKIE_SIGNIN_TOKEN);
            setIsAuthenticated(false);
          }, expiresInMs);

          // Set the cookie with its expiration
          // Note: Cookies.set 'expires' is in days, so convert expiresInMs to days
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
    },
    [setUser, setIsAuthenticated, setMessage, setError] // Dependencies: all state setters and error context functions
  );

  const logOut = useCallback(
    async () => {
      try {
        await authService.logoutRequest();
        Cookies.remove(COOKIE_SIGNIN_TOKEN);
        setIsAuthenticated(false);
        setUser(null);
      } catch (error: unknown) {
        setError(error as AxiosError);
      }
    },
    [setIsAuthenticated, setUser, setError] // Dependencies: state setters and error context function
  );

  // --- Context value object memoized with useMemo ---
  const contextValue = useMemo(
    () => ({
      signIn,
      logOut,
      user,
      isAuthenticated,
    }),
    [signIn, logOut, user, isAuthenticated] // Dependencies: memoized functions and state variables
  );

  return (
    <AuthContext.Provider value={contextValue}>{children}</AuthContext.Provider>
  );
};
