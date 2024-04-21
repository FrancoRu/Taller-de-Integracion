export interface IUser {
	id: string
	username: string
	email: string
}

interface authSignUp extends UserLoginRequest {
	name: string
	confirmPassword: string
	role: number
}

interface UserLoginRequest {
	userName: string
	password: string
}

export interface IAuthContextProps {
	signUp: (value: authSignUp) => Promise<void>
	signIn: (value: authSignIn) => Promise<void>
	logOut: () => Promise<void>
	user: IUser | null
	isAuthenticated: boolean
}
