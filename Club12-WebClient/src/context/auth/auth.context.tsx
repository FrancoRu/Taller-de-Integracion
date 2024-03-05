import React, { createContext, useState, useEffect } from 'react'
import Cookies from 'js-cookie'
import { useError } from '../../hooks/error/useError'
import { authService } from '../../services/auths/authService'
import {
	IAuthContextProps,
	IUser,
	authSignIn,
	authSignUp
} from '../../types/auths/auth'
import { checkToken } from '../../hooks/auth/useAuth'

export const AuthContext = createContext<IAuthContextProps | undefined>(
	undefined
)

export const AuthProvider: React.FC<ProviderProps> = ({ children }) => {
	const [user, setUser] = useState<IUser | null>(null)

	const [isAuthenticated, setIsAuthenticated] = useState<boolean>(false)
	const { setError } = useError()

	useEffect(() => {
		setIsAuthenticated(user !== null)
	}, [user])

	useEffect(() => {
		checkToken()
			.then((res) => {
				if (!res) {
					setIsAuthenticated(false)
					setUser(null)
				} else {
					setIsAuthenticated(true)
					setUser(res)
				}
			})
			.catch((error) => {
				setUser(null)
				setError(error)
			})
	}, [])

	const signUp = async (user: authSignUp) => {
		try {
			const res = await authService.registerRequest(user)
			const { _id, username, email } = res.data.user
			const userData: IUser = { id: _id, username: username, email: email }
			setUser(userData)
		} catch (error: unknown) {
			setError(error)
		}
	}

	const signIn = async (user: authSignIn) => {
		try {
			const res = await authService.loginRequest(user)
			const { _id, username, email } = res.data.user
			const userData: IUser = { id: _id, username: username, email: email }
			setUser(userData)
		} catch (error: unknown) {
			setError(error)
		}
	}

	const logOut = async () => {
		try {
			await authService.logoutRequest() //--> Cambiar al endpoint que elimine desde el back la sesion y desautorice el token
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
