import { UserLoginRequest, authSignUp } from '../../types/auths/auth.d'
import { sendGet, sendPost } from '../../utils/utils'

const authServiceResource = ''

export const authService = {
	registerRequest: async (user: authSignUp) =>
		sendPost(authServiceResource, user),

	loginRequest: (user: UserLoginRequest) => sendPost(`${authServiceResource}/login`, user),

	logoutRequest: () => sendGet(`${authServiceResource}/logout`),

	verifyTokenRequest: () => sendGet(`${authServiceResource}/verifyToken`)
	// verifyTokenRequest : () => axios.get(`api/verifyToken`),

	// GetLogOutRequest : () => axios.get('api/logout'),
}
