import { AxiosResponse } from 'axios';
import routes from '@/modules/core/constants/routes';
import { sendPost } from '@/modules/core/utils/axiosUtils';
import {
  ActivateAccountRequest,
  AuthResponse,
  InviteUserRequest,
  InviteUserResponse,
  LogInUserRequest,
  PasswordResetConfirmRequest,
  RefreshTokenRequest,
  RequestPasswordResetRequest,
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

  /**
   * HU-09: invites a user by email (Admin/Owner). The backend creates a
   * passwordless account and emails a magic activation link.
   */
  inviteRequest: (
    payload: InviteUserRequest
  ): Promise<AxiosResponse<InviteUserResponse> | undefined> =>
    sendPost<InviteUserResponse>(`${routes.auth}/invite`, payload),

  /**
   * HU-09: consumes the activation token from the invitation email and sets
   * the invited user's first password.
   */
  activateRequest: (
    payload: ActivateAccountRequest
  ): Promise<AxiosResponse<AuthResponse> | undefined> =>
    sendPost<AuthResponse>(`${routes.auth}/activate`, payload),

  /**
   * HU-10: self-service. Requests a password-reset magic link for the given
   * email. Always resolves 200 (no account enumeration).
   */
  requestPasswordResetRequest: (
    payload: RequestPasswordResetRequest
  ): Promise<AxiosResponse<void> | undefined> =>
    sendPost<void>(`${routes.auth}/password-reset/request`, payload),

  confirmPasswordResetRequest: (
    payload: PasswordResetConfirmRequest
  ): Promise<AxiosResponse<AuthResponse> | undefined> =>
    sendPost<AuthResponse>(`${routes.auth}/password-reset/confirm`, payload),

  logoutRequest: () => sendPost(`${routes.auth}/logout`),
};
