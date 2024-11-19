/**
 * Represents the response object for authentication tokens.
 * @interface TokenResponse
 */
export interface TokenResponse {
  /**
   * The access token used for authentication.
   * @type {string | null}
   */
  accessToken: string | null;

  /**
   * The expiration duration of the access token in date-span format.
   * @type {string}
   */
  expiresIn: string;

  /**
   * The refresh token used for refreshing the access token.
   * @type {string | null}
   */
  refreshToken: string | null;
}

/**
 * Represents the request object to refresh the access token using the refresh token.
 * @interface RefreshTokenRequest
 */
export interface RefreshTokenRequest {
  /**
   * The refresh token used to get a new access token.
   * @type {string | null}
   */
  refreshToken: string | null;
}

/**
 * Represents a user login request.
 * @interface LogInUserRequest
 */
export interface LogInUserRequest {
  /**
   * The username of the user.
   * @type {string}
   * @minLength 1
   */
  username: string;

  /**
   * The password of the user.
   * @type {string}
   * @minLength 1
   */
  password: string;
}

/**
 * Represents the response containing user authentication information.
 * @interface AuthResponse
 */
export interface AuthResponse {
  /**
   * The access token used for authentication.
   * @type {string}
   */
  accessToken: string;

  /**
   * The expiration duration of the access token in date-span format.
   * @type {string}
   */
  expiresIn: string;

  /**
   * The refresh token used to refresh the access token.
   * @type {string}
   */
  refreshToken: string;
}

/**
 * Represents the authentication context properties for sign-in, sign-out, and user information.
 * @interface IAuthContextProps
 */
export interface IAuthContextProps {
  /**
   * Sign-in method that attempts to authenticate the user.
   * @param value The login credentials.
   * @returns {Promise<boolean>} Whether authentication was successful.
   */
  signIn: (value: LogInUserRequest) => Promise<boolean>;

  /**
   * Log out the current user and clear authentication state.
   * @returns {Promise<void>} Returns a promise that resolves when logout is complete.
   */
  logOut: () => Promise<void>;

  /**
   * The current authenticated user or null if no user is logged in.
   * @type {IUser | null}
   */
  user: IUser | null;

  /**
   * Boolean flag indicating whether the user is authenticated.
   * @type {boolean}
   */
  isAuthenticated: boolean;
}

/**
 * Represents the authenticated user details.
 * @interface IUser
 */
export interface IUser {
  /**
   * The username of the user.
   * @type {string}
   */
  username: string;

  /**
   * The authentication response containing the user's access token.
   * @type {AuthResponse}
   */
  accessToken: AuthResponse;
}