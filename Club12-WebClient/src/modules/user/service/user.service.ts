import { AxiosResponse } from 'axios';
import routes from '@/modules/core/constants/routes';
import { withTablePageSize } from '@/modules/core/constants/pagination';
import { GenericResponsePagination, GUID } from '@/modules/core/types/types';
import {
  sendDelete,
  sendGet,
  sendPost,
  sendPut,
} from '@/modules/core/utils/axiosUtils';
import {
  ChangePasswordRequest,
  RegisterUserRequest,
  UpdateUserRequest,
  UserFilterRequest,
  UserResponse,
} from '@/modules/user/type/user';

export const userService = {
  getAllUsers: async (
    filters: UserFilterRequest
  ): Promise<AxiosResponse<GenericResponsePagination<UserResponse>>> =>
    await sendGet(routes.users, withTablePageSize(filters)),

  getById: async (id: GUID): Promise<AxiosResponse<UserResponse>> =>
    await sendGet(`${routes.users}/${id}`),

  createUser: async (
    data: RegisterUserRequest
  ): Promise<AxiosResponse<UserResponse>> =>
    await sendPost<UserResponse>(`${routes.auth}/register`, data),

  updateUser: async (
    id: GUID,
    data: UpdateUserRequest
  ): Promise<AxiosResponse<UserResponse>> =>
    await sendPut<UserResponse>(`${routes.users}/${id}`, data),

  resetUserPassword: async (id: GUID): Promise<AxiosResponse<void>> =>
    await sendPost<void>(`${routes.users}/${id}/password/reset`),

  changeUserPassword: async (
    id: GUID,
    data: ChangePasswordRequest
  ): Promise<AxiosResponse<void>> =>
    await sendPut<void>(`${routes.users}/${id}/password`, data),

  deleteUser: async (id: GUID): Promise<AxiosResponse<void>> =>
    await sendDelete<void>(`${routes.users}/${id}`),

  setUserActive: async (
    id: GUID,
    isActive: boolean
  ): Promise<AxiosResponse<UserResponse>> =>
    await sendPut<UserResponse>(`${routes.users}/${id}/active`, { isActive }),
};
