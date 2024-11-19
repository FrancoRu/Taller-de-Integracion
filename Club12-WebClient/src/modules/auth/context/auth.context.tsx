import { AxiosError } from "axios";
import Cookies from "js-cookie";
import React, { createContext, useState } from "react";
import { ProviderProps } from "../../core/types/types";
import { useError } from "../../error/hooks/error.hock";
import { authService } from "../service/auth.service";
import {
  AuthResponse,
  IAuthContextProps,
  IUser,
  UserLoginRequest,
} from "../type/auth";

export const AuthContext = createContext<IAuthContextProps | undefined>(
  undefined
);

export const AuthProvider: React.FC<ProviderProps> = ({ children }) => {
  const [user, setUser] = useState<IUser | null>(null);

  const service = authService;
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(false);
  const { setError, setMessage } = useError();

  const signIn = async (user: UserLoginRequest): Promise<boolean> => {
    try {
      const res = await service.loginRequest(user);
      if (
        res?.status === 200 &&
        res?.data
        //&& res.data?.accessToken
      ) {
        setUser({
          userName: user.userName,
          accessToken: res?.data as AuthResponse,
        });
        console.log(res);
        const expiresIn = res.data.expiresIn.split(":").map(Number);
        const expiresInMs =
          expiresIn[0] * 3600 * 1000 +
          expiresIn[1] * 60 * 1000 +
          expiresIn[2] * 1000;

        setTimeout(() => {
          Cookies.remove("Club12_SignInToken");
          setIsAuthenticated(false);
        }, expiresInMs);

        setIsAuthenticated(true);
        Cookies.set("Club12_SignInToken", res.data.accessToken, {
          expires: expiresInMs / (1000 * 60 * 60 * 24),
        });

        setMessage(res.status, ["Log In successfully"]);
        return true;
      }
    } catch (error: unknown) {
      setError(error as AxiosError);
      return false;
    }
    return false;
  };

  const logOut = async () => {
    try {
      await service.logoutRequest(); //--> Cambiar al endpoint que elimine desde el back la sesion y desautorice el token
      Cookies.remove("token");
      setIsAuthenticated(false);
      setUser(null);
    } catch (error: unknown) {
      setError(error as AxiosError);
    }
  };

  return (
    <AuthContext.Provider value={{ signIn, logOut, user, isAuthenticated }}>
      {children}
    </AuthContext.Provider>
  );
};
