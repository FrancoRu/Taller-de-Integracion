import React, { createContext, useState, useEffect } from 'react'
import Cookies from 'js-cookie'
import { useError } from '../../hooks/error/useError'
import { authService } from '../../services/auths/authService'
import {
	IAuthContextProps,
	IUser,
	UserLoginRequest,
	authSignUp
} from '../../types/auths/auth'

export const AuthContext = createContext<IAuthContextProps | undefined>(
	undefined
)

export const AuthProvider: React.FC<ProviderProps> = ({ children }) => {
	const [user, setUser] = useState<IUser | null>(null)

	const service = authService
	const [isAuthenticated, setIsAuthenticated] = useState<boolean>(false)
	const { setError } = useError()

	useEffect(() => {
		setIsAuthenticated(user !== null)
	}, [user])

	useEffect(() => {
		const checkToken = async () => {
			const cookies = Cookies.get()
			if (cookies.token) {
				try {
					const res = await service.verifyTokenRequest()
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

					if (!res.data) setIsAuthenticated(false)
					setIsAuthenticated(true)
					setUser({
						id: res.data._id,
						username: res.data.username,
						email: res.data.email
					})
				} catch (error) {
					setIsAuthenticated(false)
					setUser(null)
				}
			}
		}
		checkToken()
	}, [])

	const signUp = async (user: authSignUp) => {
		try {
			const res = await service.registerRequest(user)
			const { _id, username, email } = res.data.user
			const userData: IUser = { id: _id, username: username, email: email }
			setUser(userData)
		} catch (error: unknown) {
			setError(error)
		}
	}

	const signIn = async (user: UserLoginRequest) => {
		try {
			const res = await service.loginRequest(user)
			const { _id, username, email } = res.data.user
			const userData: IUser = { id: _id, username: username, email: email }
			setUser(userData)
		} catch (error: unknown) {
			setError(error)
		}
	}

	const logOut = async () => {
		try {
			await service.logoutRequest() //--> Cambiar al endpoint que elimine desde el back la sesion y desautorice el token
			Cookies.remove('token')
			setIsAuthenticated(false)
			setUser(null)
		} catch (error: unknown) {
			setError(error)
		}
	}

	return (
		<AuthContext.Provider
			value={{ signUp, signIn, logOut, user, isAuthenticated }}
		>
			{children}
		</AuthContext.Provider>
	)
}
