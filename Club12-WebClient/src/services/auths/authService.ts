import { IUser } from "../../types/auths/auth.d"
import { sendPost } from "../../utils/utils"
const authServiceResource = '/api/auths'


export const authService = {
    registerRequest : async (user: IUser) => sendPost(authServiceResource, user),

    loginRequest : (user: IUser) => sendPost(authServiceResource, user),

    // verifyTokenRequest : () => axios.get(`api/verifyToken`),

    // GetLogOutRequest : () => axios.get('api/logout'),
}

