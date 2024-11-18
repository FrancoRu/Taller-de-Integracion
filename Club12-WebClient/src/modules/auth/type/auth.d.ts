export interface ITokenResponse {
  accessToken: string;
  expiresIn: Date;
}

export interface IUser {
  userName: string;
  accessToken: AuthResponse;
}

interface authSignUp extends UserLoginRequest {
  name: string;
  confirmPassword: string;
  role: number;
}

interface UserLoginRequest {
  userName: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
  expiresIn: string;
  refreshToken: string;
}

export interface IAuthContextProps {
  signIn: (value: authSignIn) => Promise<boolean>;
  logOut: () => Promise<void>;
  user: IUser | null;
  isAuthenticated: boolean;
}
