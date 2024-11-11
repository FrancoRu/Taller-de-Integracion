import envVariables from "../../core/constants/envVariables";
import { sendGet, sendPost } from "../../core/utils/utilsAxios";
import { authSignUp, UserLoginRequest } from "../type/auth";

export const authService = {
  registerRequest: async (user: authSignUp) =>
    sendPost(envVariables.authUrl, user),

  loginRequest: (user: UserLoginRequest) =>
    sendPost(`${envVariables.authUrl}/login`, user),

  logoutRequest: () => sendGet(`${envVariables.authUrl}/logout`),

  verifyTokenRequest: () => sendGet(`${envVariables.authUrl}/verifyToken`),
  // verifyTokenRequest : () => axios.get(`api/verifyToken`),

  // GetLogOutRequest : () => axios.get('api/logout'),
};
