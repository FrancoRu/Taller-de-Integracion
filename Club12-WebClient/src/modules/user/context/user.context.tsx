import { AxiosError, AxiosResponse } from 'axios';
import {
  createContext,
  useCallback,
  useEffect,
  useMemo,
  useState,
} from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { ERROR_MESSAGES } from '@/modules/core/constants/constants';
import { ProviderProps } from '@/modules/core/types/types';
import { useError } from '@/modules/error/hooks/error.hock';
import { userService } from '@/modules/user/service/user.service';
import { IUserContextProps } from '@/modules/user/type/user';
import {
  ChangePasswordRequest,
  RegisterUserRequest,
  UpdateUserRequest,
  UserFilterRequest,
  UserResponse,
} from '@/modules/user/type/user';
import { userKeys } from '@/modules/user/queryKeys';

export const UserContext = createContext<IUserContextProps | undefined>(
  undefined
);

export const UserProvider: React.FC<ProviderProps> = ({ children }) => {
  const [user, setUser] = useState<UserResponse | null>(null);
  const [users, setUsers] = useState<UserResponse[] | null>(null);
  const { setError } = useError();
  const queryClient = useQueryClient();

  const handleUnknownError = useCallback(
    (error: unknown) => {
      if (error instanceof AxiosError) {
        setError(error);
        return;
      }

      setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
    },
    [setError]
  );

  useEffect(() => {
    if (!user) {
      return;
    }

    setUsers(prev => {
      const list = prev ?? [];
      const index = list.findIndex(u => u.userId === user.userId);
      if (index === -1) return [...list, user];
      const updated = [...list];
      updated[index] = user;
      return updated;
    });
  }, [user]);

  const getAllUsers = useCallback(
    async (filters: UserFilterRequest) => {
      try {
        const res = await queryClient.fetchQuery({
          queryKey: userKeys.list(filters),
          queryFn: async () => await userService.getAllUsers(filters),
        });

        if (res?.data?.items) {
          setUsers(res.data.items);
          return res.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [handleUnknownError, queryClient]
  );

  const getById = useCallback(
    async (id: UserResponse['userId']) => {
      try {
        const res: AxiosResponse<UserResponse> = await queryClient.fetchQuery({
          queryKey: userKeys.byId(id),
          queryFn: async () => await userService.getById(id),
        });

        if (res?.data) {
          setUser(res.data);
          return res.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [handleUnknownError, queryClient]
  );

  const createUser = useCallback(
    async (data: RegisterUserRequest) => {
      try {
        const res: AxiosResponse<UserResponse> =
          await userService.createUser(data);
        if (res?.data) {
          setUser(res.data);
          return res.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [handleUnknownError]
  );

  const updateUser = useCallback(
    async (id: UserResponse['userId'], data: UpdateUserRequest) => {
      try {
        const res: AxiosResponse<UserResponse> = await userService.updateUser(
          id,
          data
        );
        if (res?.data) {
          setUser(res.data);
          return res.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [handleUnknownError]
  );

  const resetUserPassword = useCallback(
    async (id: UserResponse['userId']): Promise<boolean> => {
      try {
        await userService.resetUserPassword(id);
        return true;
      } catch (error: unknown) {
        handleUnknownError(error);
        return false;
      }
    },
    [handleUnknownError]
  );

  const changeUserPassword = useCallback(
    async (
      id: UserResponse['userId'],
      data: ChangePasswordRequest
    ): Promise<boolean> => {
      try {
        await userService.changeUserPassword(id, data);
        return true;
      } catch (error: unknown) {
        handleUnknownError(error);
        return false;
      }
    },
    [handleUnknownError]
  );

  const deleteUser = useCallback(
    async (id: UserResponse['userId']): Promise<boolean> => {
      try {
        await userService.deleteUser(id);
        setUsers(prev => prev?.filter(u => u.userId !== id) ?? null);
        setUser(prev => (prev?.userId === id ? null : prev));
        queryClient.removeQueries({ queryKey: userKeys.byId(id) });
        await queryClient.invalidateQueries({ queryKey: userKeys.list() });
        return true;
      } catch (error: unknown) {
        handleUnknownError(error);
        return false;
      }
    },
    [handleUnknownError, queryClient]
  );

  const setUserActive = useCallback(
    async (
      id: UserResponse['userId'],
      isActive: boolean
    ): Promise<UserResponse | void> => {
      try {
        const res: AxiosResponse<UserResponse> =
          await userService.setUserActive(id, isActive);
        if (res?.data) {
          setUser(res.data);
          return res.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [handleUnknownError]
  );

  const contextValue = useMemo(
    () => ({
      user,
      users,
      getAllUsers,
      getById,
      createUser,
      updateUser,
      resetUserPassword,
      changeUserPassword,
      deleteUser,
      setUserActive,
    }),
    [
      user,
      users,
      getAllUsers,
      getById,
      createUser,
      updateUser,
      resetUserPassword,
      changeUserPassword,
      deleteUser,
      setUserActive,
    ]
  );

  return (
    <UserContext.Provider value={contextValue}>{children}</UserContext.Provider>
  );
};
