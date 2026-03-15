import { AxiosError } from 'axios';
import Cookies from 'js-cookie';
import { decodeToken } from 'react-jwt';
import React, {
  createContext,
  useRef,
  useState,
  useEffect,
  useCallback,
  useMemo,
} from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
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
  JWT,
} from '../../core/constants/constants';
import { UserRolesType } from '../../core/enum/user/userRolesType';

export const AuthContext = createContext<IAuthContextProps | undefined>(
  undefined
);

const ROLE_CLAIM =
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

const parseExpiresInToMs = (expiresIn: string): number => {
  const [dayPart, timePart] = expiresIn.includes('.')
    ? expiresIn.split('.')
    : [undefined, expiresIn];
  const [hours = 0, minutes = 0, seconds = 0] = timePart.split(':').map(Number);
  const days = dayPart ? Number(dayPart) : 0;

  if ([days, hours, minutes, seconds].some(Number.isNaN)) {
    return 0;
  }

  return (
    days * 24 * EXPIRATION_TIME.MS_IN_HOUR +
    hours * EXPIRATION_TIME.MS_IN_HOUR +
    minutes * EXPIRATION_TIME.MS_IN_MINUTE +
    seconds * EXPIRATION_TIME.MS_IN_SECOND
  );
};

const getUserRoleFromToken = (accessToken: string): UserRolesType => {
  const payload = decodeToken<Record<string, unknown>>(accessToken);
  if (!payload) {
    return UserRolesType.Guest;
  }

  const roleClaim = payload[ROLE_CLAIM] ?? payload.role ?? payload.roles;
  const roleValues = Array.isArray(roleClaim) ? roleClaim : [roleClaim];
  const validRoles = new Set(Object.values(UserRolesType));

  const role = roleValues.find(
    (currentRole): currentRole is UserRolesType =>
      typeof currentRole === 'string' &&
      validRoles.has(currentRole as UserRolesType)
  );

  return role ?? UserRolesType.Guest;
};

export const AuthProvider: React.FC<ProviderProps> = ({ children }) => {
  const [user, setUser] = useState<IUser | null>(null);
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(false);
  const authTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const { setError, setMessage } = useError();
  const queryClient = useQueryClient();

  const { data: hasToken } = useQuery({
    queryKey: ['auth', 'has-token'],
    queryFn: async () => Boolean(Cookies.get(COOKIE_SIGNIN_TOKEN)),
    staleTime: Infinity,
  });

  const signInMutation = useMutation({
    mutationFn: authService.loginRequest,
  });

  const logOutMutation = useMutation({
    mutationFn: authService.logoutRequest,
  });

  const refreshTokenMutation = useMutation({
    mutationFn: authService.refreshTokenRequest,
  });

  const clearAuthTimeout = useCallback(() => {
    if (authTimeoutRef.current) {
      clearTimeout(authTimeoutRef.current);
      authTimeoutRef.current = null;
    }
  }, []);

  const clearAuthStorage = useCallback(() => {
    Cookies.remove(COOKIE_SIGNIN_TOKEN);
    localStorage.removeItem(JWT.REFRESH_TOKEN);
    setIsAuthenticated(false);
    queryClient.setQueryData(['auth', 'has-token'], false);
  }, [queryClient]);

  const applyAuthData = useCallback(
    (authData: AuthResponse, username?: string) => {
      const userRole = getUserRoleFromToken(authData.accessToken);
      const expiresInMs = parseExpiresInToMs(authData.expiresIn);

      setUser(prevUser => ({
        username: username ?? prevUser?.username ?? '',
        accessToken: authData,
        role: userRole,
      }));

      Cookies.set(
        COOKIE_SIGNIN_TOKEN,
        authData.accessToken,
        expiresInMs > 0
          ? { expires: expiresInMs / (1000 * 60 * 60 * 24) }
          : undefined
      );

      if (authData.refreshToken) {
        localStorage.setItem(JWT.REFRESH_TOKEN, authData.refreshToken);
      } else {
        localStorage.removeItem(JWT.REFRESH_TOKEN);
      }

      setIsAuthenticated(true);
      queryClient.setQueryData(['auth', 'has-token'], true);

      return expiresInMs;
    },
    [queryClient]
  );

  const refreshAuthToken = useCallback(async (): Promise<boolean> => {
    const refreshToken = localStorage.getItem(JWT.REFRESH_TOKEN);
    if (!refreshToken) {
      clearAuthTimeout();
      clearAuthStorage();
      return false;
    }

    try {
      const res = await refreshTokenMutation.mutateAsync({ refreshToken });
      if (res?.status === 200 && res?.data) {
        const expiresInMs = applyAuthData(res.data as AuthResponse);

        clearAuthTimeout();
        if (expiresInMs > 0) {
          authTimeoutRef.current = setTimeout(() => {
            void refreshAuthToken();
          }, expiresInMs);
        }

        return true;
      }
    } catch (error: unknown) {
      setError(error as AxiosError);
    }

    clearAuthTimeout();
    clearAuthStorage();
    setUser(null);
    return false;
  }, [
    refreshTokenMutation,
    applyAuthData,
    clearAuthStorage,
    clearAuthTimeout,
    setError,
  ]);

  useEffect(() => {
    setIsAuthenticated(Boolean(hasToken));
  }, [hasToken]);

  const signIn = useCallback(
    async (userData: LogInUserRequest): Promise<boolean> => {
      try {
        const res = await signInMutation.mutateAsync(userData);
        if (res?.status === 200 && res?.data) {
          const authData = res.data as AuthResponse;
          const expiresInMs = applyAuthData(authData, userData.username);

          clearAuthTimeout();
          if (expiresInMs > 0) {
            authTimeoutRef.current = setTimeout(() => {
              void refreshAuthToken();
            }, expiresInMs);
          }

          setMessage(res.status, [SUCCESS_MESSAGES.LOGIN_SUCCESS]);
          return true;
        }
      } catch (error: unknown) {
        setError(error as AxiosError);
        return false;
      }
      return false;
    },
    [
      setMessage,
      setError,
      signInMutation,
      applyAuthData,
      clearAuthTimeout,
      refreshAuthToken,
    ]
  );

  const logOut = useCallback(async () => {
    try {
      await logOutMutation.mutateAsync();
      clearAuthTimeout();
      clearAuthStorage();
      setUser(null);
    } catch (error: unknown) {
      setError(error as AxiosError);
    }
  }, [clearAuthStorage, clearAuthTimeout, setUser, setError, logOutMutation]);

  useEffect(() => {
    return () => {
      clearAuthTimeout();
    };
  }, [clearAuthTimeout]);

  useEffect(() => {
    if (!hasToken) {
      return;
    }

    const token = Cookies.get(COOKIE_SIGNIN_TOKEN);
    if (!token) {
      return;
    }

    setUser(prevUser => {
      if (prevUser?.accessToken?.accessToken === token) {
        return prevUser;
      }

      const role = getUserRoleFromToken(token);
      return {
        username: prevUser?.username ?? '',
        accessToken: {
          accessToken: token,
          expiresIn: prevUser?.accessToken?.expiresIn ?? '00:00:00',
          refreshToken: prevUser?.accessToken?.refreshToken ?? null,
        },
        role,
      };
    });
  }, [hasToken]);

  const contextValue = useMemo(
    () => ({
      signIn,
      logOut,
      user,
      isAuthenticated,
      role: user?.role ?? UserRolesType.Guest,
    }),
    [signIn, logOut, user, isAuthenticated]
  );

  return (
    <AuthContext.Provider value={contextValue}>{children}</AuthContext.Provider>
  );
};
