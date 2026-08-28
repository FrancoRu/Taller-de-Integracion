import { UserRolesType } from '@/modules/core/enum/user/userRolesType';

/**
 * Represents the response object for authentication tokens.
 * @interface TokenResponse
 */
export interface TokenResponse {
  /**
   * The access token used for authentication.
   * @type {string | null}
   */
  accessToken: string;

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
   * @type {string}
   */
  refreshToken: string;
}

/**
 * Represents a password reset confirmation request from an email link.
 * @interface PasswordResetConfirmRequest
 */
export interface PasswordResetConfirmRequest {
  /**
   * Email associated with the user account.
   * @type {string}
   */
  email: string;

  /**
   * Token received by email for password reset.
   * @type {string}
   */
  token: string;

  /**
   * New password to set.
   * @type {string}
   */
  newPassword: string;
}

/**
 * HU-09: request to invite a user by email only (no password). The backend
 * creates a passwordless account and emails a magic activation link. Requires
 * Admin or Owner.
 * @interface InviteUserRequest
 */
export interface InviteUserRequest {
  /**
   * Email the invitation/activation link is sent to.
   * @type {string}
   */
  email: string;

  /**
   * Optional contact phone number.
   * @type {string | undefined}
   */
  phone?: string;

  /**
   * Target role. Accepted values: ADMIN, OWNER.
   * @type {string}
   */
  role: string;
}

/**
 * HU-09: confirmation payload returned after inviting a user by email.
 * @interface InviteUserResponse
 */
export interface InviteUserResponse {
  /**
   * Id of the newly created (passwordless) user.
   * @type {string}
   */
  userId: string;

  /**
   * Email the activation link was sent to.
   * @type {string}
   */
  email: string;

  /**
   * Assigned role.
   * @type {string}
   */
  role: string;
}

/**
 * HU-09: payload the invited user submits from the activation email link to
 * set their first password and enable login.
 * @interface ActivateAccountRequest
 */
export interface ActivateAccountRequest {
  /**
   * Email associated with the invited account.
   * @type {string}
   */
  email: string;

  /**
   * Activation token received by email.
   * @type {string}
   */
  token: string;

  /**
   * First password the user chooses.
   * @type {string}
   */
  newPassword: string;
}

/**
 * HU-10: self-service request to receive a password-reset magic link by email.
 * @interface RequestPasswordResetRequest
 */
export interface RequestPasswordResetRequest {
  /**
   * Email to send the password-reset link to.
   * @type {string}
   */
  email: string;
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
  email: string;

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
export type AuthResponse = TokenResponse;

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

  /**
   * The role of the current user.
   * @type {UserRolesType}
   */
  role: UserRolesType;
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

  /**
   * The role of the user.
   * @type {UserRolesType}
   */
  role: UserRolesType;
}
