import { UserRolesType } from '@/modules/core/enum/user/userRolesType';
import {
  Filtered,
  GenericResponsePagination,
  GUID,
} from '@/modules/core/types/types';

export interface UserResponse {
  userId: GUID;
  username: string;
  email: string;
  phoneNumber?: string;
  role: UserRolesType;
  createdByOwnerId?: GUID;
  isActive: boolean;
}

export interface RegisterUserRequest {
  email: string;
  username: string;
  phone?: string;
  role: string;
}

export interface RegisterUserResponse {
  userId: GUID;
  email: string;
  username: string;
  role: string;
  phoneNumber?: string;
}

export interface UpdateUserRequest {
  username?: string;
  email?: string;
  phone?: string;
  /**
   * Optional. When set, replaces the target user's role. Only ADMIN and
   * OWNER may set this, and the caller's exact assignment policy (plus the
   * guard against changing your own role) is enforced server-side.
   */
  role?: UserRolesType;
}

export interface ChangePasswordRequest {
  newPassword: string;
  currentPassword?: string;
}

export interface IUserContextProps {
  user: UserResponse | null;
  users: UserResponse[] | null;
  getAllUsers: (
    filters: UserFilterRequest
  ) => Promise<GenericResponsePagination<UserResponse> | void>;
  getById: (id: GUID) => Promise<UserResponse | void>;
  createUser: (
    data: RegisterUserRequest
  ) => Promise<RegisterUserResponse | void>;
  updateUser: (
    id: GUID,
    data: UpdateUserRequest
  ) => Promise<UserResponse | void>;
  resetUserPassword: (id: GUID) => Promise<boolean>;
  changeUserPassword: (
    id: GUID,
    data: ChangePasswordRequest
  ) => Promise<boolean>;
  deleteUser: (id: GUID) => Promise<boolean>;
  setUserActive: (id: GUID, isActive: boolean) => Promise<UserResponse | void>;
}

export interface UserFilterRequest extends Filtered {
  username?: string;
  email?: string;
  phoneNumber?: string;
  role?: UserRolesType;
}
