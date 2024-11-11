import { createContext, useEffect, useState } from 'react'
import { AxiosError } from 'axios'
import Swal from 'sweetalert2'

interface ErrorContextProp {
	errors: string[] | null
	setError: (value: unknown) => void
	setMessage: (status: number, message: string[]) => void
}

export const ErrorContext = createContext<ErrorContextProp | undefined>(
	undefined
)

export const ErrorProvider: React.FC<ProviderProps> = ({ children }) => {
	const [errors, setErrors] = useState<string[] | null>(null)

	const setError = (error: unknown) => {
		if (error instanceof AxiosError && error.response) {
			setErrors(error.response.data.error)
			setMessage(error.response.status, error.response.data.error)
		} else {
			console.error('Error: ', error)
		}
	}

	const setMessage = (status: number, message: string[]) => {
		const stat = status < 400 ? 'success' : 'error'
		const messages = message.join(', ')
		Swal.fire({
			position: 'center',
			icon: stat,
			title: messages,
			showConfirmButton: false,
			timer: 1500,
			color: 'black'
		})
	}

	useEffect(() => {
		if (errors !== null) {
			const timer = setTimeout(() => {
				setErrors(null)
			}, 5000)
			return () => clearTimeout(timer)
		}
	}, [errors])

	return (
		<ErrorContext.Provider value={{ errors, setError, setMessage }}>
			{children}
		</ErrorContext.Provider>
	)
}
