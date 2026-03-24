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
import { ProviderProps } from '../../core/types/types';
import { useError } from '../../error/hooks/error.hock';
import { userService } from '../service/user.service';
import { IUserContextProps } from '../type/user';
import {
  ChangePasswordRequest,
  RegisterUserRequest,
  UpdateUserRequest,
  UserFilterRequest,
  UserResponse,
} from '../type/user';

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
          queryKey: ['user', 'list', filters],
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
          queryKey: ['user', 'byId', id],
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
    ]
  );

  return (
    <UserContext.Provider value={contextValue}>{children}</UserContext.Provider>
  );
};
