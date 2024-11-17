import routes from "../../core/constants/envVariables";
import { sendGet, sendPost } from "../../core/utils/utilsAxios";
import { authSignUp, UserLoginRequest } from "../type/auth";

export const authService = {
  registerRequest: async (user: authSignUp) => sendPost(routes.users, user),

  loginRequest: (user: UserLoginRequest) =>
    sendPost(`${routes.users}/login`, user),

  logoutRequest: () => sendGet(`${routes.users}/logout`),

  verifyTokenRequest: () => sendGet(`${routes.users}/verifyToken`),
  // verifyTokenRequest : () => axios.get(`api/verifyToken`),

  // GetLogOutRequest : () => axios.get('api/logout'),
};
