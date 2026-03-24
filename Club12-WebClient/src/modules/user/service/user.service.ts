import { AxiosResponse } from 'axios';
import routes from '../../core/constants/routes';
import { GenericResponsePagination, GUID } from '../../core/types/types';
import { sendGet, sendPost, sendPut } from '../../core/utils/axiosUtils';
import {
  ChangePasswordRequest,
  RegisterUserRequest,
  UpdateUserRequest,
  UserFilterRequest,
  UserResponse,
} from '../type/user';

export const userService = {
  getAllUsers: async (
    filters: UserFilterRequest
  ): Promise<AxiosResponse<GenericResponsePagination<UserResponse>>> =>
    await sendGet(routes.users, filters),

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
};
