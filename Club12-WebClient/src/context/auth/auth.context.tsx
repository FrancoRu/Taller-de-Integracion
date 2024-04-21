import React, { createContext, useState, useEffect } from "react";
import Cookies from "js-cookie";
import { useError } from "../../hooks/error/useError";
import { authService } from "../../services/auths/authService";
import {
  IAuthContextProps,
  ITokenResponse,
  IUser,
  UserLoginRequest,
} from "../../types/auths/auth";
import { AxiosError } from "axios";

export const AuthContext = createContext<IAuthContextProps | undefined>(
  undefined
);

export const AuthProvider: React.FC<ProviderProps> = ({ children }) => {
  const [user, setUser] = useState<IUser | null>(null);

  const service = authService;
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(false);
  const { setError, setMessage } = useError();

  useEffect(() => {
    setIsAuthenticated(user !== null);
  }, [user]);

  useEffect(() => {
    const cookie = Cookies.get("token");
    if (cookie) {
      const data: IUser = JSON.parse(cookie);
      setIsAuthenticated(true);
      setUser({
        userName: data.userName,
        accessToken: data.accessToken,
      });
    }
  }, []);

  //REVIEW:
  // useEffect(()  {
  // 	const checkToken = async () => {
  // 		const cookies = Cookies.get()
  // 		if (cookies.token) {
  // 			try {
  // 				// const res = await service.verifyTokenRequest()
  // 				/**
  // 				 * TODO
  // 				 * Se debe implementar el verificar token todavia esto es un ejemplo
  // 				 * {
  // 					data: {
  // 						_id: 'GUID',
  // 						username: 'example',
  // 						email: 'example@example.com'
  // 					}
  // 				 *
  // 				*/

  // 				// if (!res.data) setIsAuthenticated(false)
  // 				setIsAuthenticated(true)
  // 				const token: ITokenResponse = JSON.parse(cookies.token) as ITokenResponse
  // 				alert(token)
  // 			} catch (error) {
  // 				setIsAuthenticated(false)
  // 				setUser(null)
  // 			}
  // 		}
  // 	}
  // 	checkToken()
  // }, [])

  const signIn = async (user: UserLoginRequest) => {
    try {
      const res = await service.loginRequest(user);
      if (res?.status === 200) {
        setUser({
          userName: user.userName,
          accessToken: res?.data as ITokenResponse,
        });
        setIsAuthenticated(true);
        Cookies.set("token", JSON.stringify(user), {
          expires: 1 / 24,
        });
        setMessage(res.status, ["Log In successfully"]);
      }
    } catch (error: unknown) {
      setError(error as AxiosError);
    }
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
