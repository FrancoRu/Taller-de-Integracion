import { useContext } from 'react'
import { AuthContext } from '../../context/auth/auth.context'
import Cookies from 'js-cookie'
import { authService } from '../../services/auths/authService'
import { IUser } from '../../types/auths/auth'

export const useAuth = () => {
	const context = useContext(AuthContext)
	if (!context) {
		throw new Error('useAuth must be used whithin an Auth Provider')
	}
	return context
}

export const checkToken = async (): Promise<boolean | IUser | any> => {
	const cookies = Cookies.get()
	if (cookies.token) {
		try {
			/**
			 * TODO
			 * Se debe implementar el verificar token todavia esto es un ejemplo
			 * {
				data: {
					_id: 'GUID',
					username: 'example',
					email: 'example@example.com'
				}
			 * 
			*/
			const res = await authService.verifyTokenRequest()
			if (!res.data) return false
			return {
				id: res.data._id,
				username: res.data.username,
				email: res.data.email
			} as IUser
		} catch (error: any) {
			return error
		}
	}
	return false
}
