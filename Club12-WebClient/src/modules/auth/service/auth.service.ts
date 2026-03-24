import { AxiosResponse } from 'axios';
import routes from '../../core/constants/routes';
import { sendPost } from '../../core/utils/axiosUtils';
import {
  AuthResponse,
  LogInUserRequest,
  PasswordResetConfirmRequest,
  RefreshTokenRequest,
} from '../type/auth';

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
