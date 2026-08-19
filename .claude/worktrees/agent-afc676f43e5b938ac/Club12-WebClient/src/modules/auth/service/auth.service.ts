import { AxiosResponse } from 'axios';
import routes from '@/modules/core/constants/routes';
import { sendPost } from '@/modules/core/utils/axiosUtils';
import {
  AuthResponse,
  LogInUserRequest,
  PasswordResetConfirmRequest,
  RefreshTokenRequest,
} from '@/modules/auth/type/auth';

export const authService = {
  loginRequest: (
    user: LogInUserRequest
  ): Promise<AxiosResponse<AuthResponse> | undefined> =>
    sendPost<AuthResponse>(`${routes.auth}/login`, user),

  refreshTokenRequest: (
    refreshToken: RefreshTokenRequest
  ): Promise<AxiosResponse<AuthResponse> | undefined> =>
    sendPost<AuthResponse>(`${routes.auth}/refresh-token`, refreshToken),

  confirmPasswordResetRequest: (
    payload: PasswordResetConfirmRequest
  ): Promise<AxiosResponse<AuthResponse> | undefined> =>
    sendPost<AuthResponse>(`${routes.auth}/password-reset/confirm`, payload),

  logoutRequest: () => sendPost(`${routes.auth}/logout`),
};
