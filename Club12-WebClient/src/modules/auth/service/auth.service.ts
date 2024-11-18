import { AxiosResponse } from "axios";
import routes from "../../core/constants/routes";
import { sendGet, sendPost } from "../../core/utils/utilsAxios";
import { AuthResponse, authSignUp, UserLoginRequest } from "../type/auth";

export const authService = {
  registerRequest: async (user: authSignUp) => sendPost(routes.users, user),

  loginRequest: (
    user: UserLoginRequest
  ): Promise<AxiosResponse<AuthResponse> | undefined> =>
    sendPost<AuthResponse>(`${routes.users}/login`, user),

  logoutRequest: () => sendGet(`${routes.users}/logout`),

  verifyTokenRequest: () => sendGet(`${routes.users}/verifyToken`),
  // verifyTokenRequest : () => axios.get(`api/verifyToken`),

  // GetLogOutRequest : () => axios.get('api/logout'),
};
