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
import { ProviderProps } from '@/modules/core/types/types';
import { useError } from '@/modules/error/hooks/error.hock';
import { authService } from '@/modules/auth/service/auth.service';
import {
  AuthResponse,
  IAuthContextProps,
  IUser,
  LogInUserRequest,
} from '@/modules/auth/type/auth';

import {
  COOKIE_SIGNIN_TOKEN,
  SUCCESS_MESSAGES,
  EXPIRATION_TIME,
  JWT,
} from '@/modules/core/constants/constants';
import { UserRolesType } from '@/modules/core/enum/user/userRolesType';
import { authKeys } from '@/modules/auth/queryKeys';
import { HttpStatus } from '@/modules/core/constants/httpStatus';

export const AuthContext = createContext<IAuthContextProps | undefined>(
  undefined
);

const ROLE_CLAIM =
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
const ROLE_CLAIM_LEGACY =
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role';

const ROLE_NORMALIZATION_MAP: Record<string, UserRolesType> = {
  admin: UserRolesType.Admin,
  administrador: UserRolesType.Admin,
  owner: UserRolesType.Owner,
  duenio: UserRolesType.Owner,
  dueño: UserRolesType.Owner,
  tournament_manager: UserRolesType.TournamentManager,
  tournamentmanager: UserRolesType.TournamentManager,
  'tournament manager': UserRolesType.TournamentManager,
  team_manager: UserRolesType.TeamManager,
  teammanager: UserRolesType.TeamManager,
  'team manager': UserRolesType.TeamManager,
  guest: UserRolesType.Guest,
};

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

  const explicitRoleClaims = [
    payload[ROLE_CLAIM],
    payload[ROLE_CLAIM_LEGACY],
    payload.role,
    payload.roles,
    payload.Role,
    payload.Roles,
  ];

  const dynamicRoleClaims = Object.entries(payload)
    .filter(([key]) => key.toLowerCase().includes('role'))
    .map(([, value]) => value);

  const rawRoleValues = [...explicitRoleClaims, ...dynamicRoleClaims].flatMap(
    value => (Array.isArray(value) ? value : [value])
  );

  const normalizedRole = rawRoleValues
    .filter((value): value is string => typeof value === 'string')
    .flatMap(value => value.split(','))
    .map(value => value.trim().toLowerCase())
    .find(value => Boolean(ROLE_NORMALIZATION_MAP[value]));

  return normalizedRole
    ? ROLE_NORMALIZATION_MAP[normalizedRole]
    : UserRolesType.Guest;
};

export const AuthProvider: React.FC<ProviderProps> = ({ children }) => {
  // Both `user` (which drives `role`) and `isAuthenticated` are seeded
  // synchronously from the cookie so route guards never see a false
  // "not authenticated" / wrong-role flash on first render or hard reload
  // while the async `hasToken` query below is still settling. That flash
  // was pushing an extra /login or /forbidden history entry via
  // PrivateRoute, breaking the browser back button.
  const [user, setUser] = useState<IUser | null>(() => {
    const token = Cookies.get(COOKIE_SIGNIN_TOKEN);
    if (!token) {
      return null;
    }

    return {
      username: '',
      accessToken: { accessToken: token, expiresIn: '00:00:00', refreshToken: null },
      role: getUserRoleFromToken(token),
    };
  });
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(() =>
    Boolean(Cookies.get(COOKIE_SIGNIN_TOKEN))
  );
  const authTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const { setError, setMessage } = useError();
  const queryClient = useQueryClient();

  const { data: hasToken } = useQuery({
    queryKey: authKeys.hasToken(),
    queryFn: async () => Boolean(Cookies.get(COOKIE_SIGNIN_TOKEN)),
    staleTime: Infinity,
    initialData: () => Boolean(Cookies.get(COOKIE_SIGNIN_TOKEN)),
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
    queryClient.setQueryData(authKeys.hasToken(), false);
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
      queryClient.setQueryData(authKeys.hasToken(), true);

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
      if (res?.status === HttpStatus.Ok && res?.data) {
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
        if (res?.status === HttpStatus.Ok && res?.data) {
          const authData = res.data as AuthResponse;
          const expiresInMs = applyAuthData(authData, userData.email);

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
