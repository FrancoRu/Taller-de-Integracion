import { AxiosResponse } from 'axios';
import routes from '../../core/constants/routes';
import { sendPost } from '../../core/utils/axiosUtils';
import {
  AuthResponse,
  LogInUserRequest,
  RefreshTokenRequest,
} from '../type/auth';

export const authService = {
  loginRequest: (
    user: LogInUserRequest
  ): Promise<AxiosResponse<AuthResponse> | undefined> =>
    sendPost<AuthResponse>(`${routes.users}/login`, user),

  refreshTokenRequest: (
    refreshToken: RefreshTokenRequest
  ): Promise<AxiosResponse<AuthResponse> | undefined> =>
    sendPost<AuthResponse>(`${routes.users}/refresh-token`, refreshToken),

  logoutRequest: () => sendPost(`${routes.users}/logout`),
};
