import React from 'react'
import { Box, Typography } from '@mui/material'

import '../../styles/auth/auth.css'

import formDataLogin from '../../data/readJSON.json'
import { Form } from '../../components/form/form'
import { BaseInputJSON } from '../../types/form/form'
import { handleFields } from '../../utils/formUtils'
import { authSignUp } from '../../types/auths/auth'
import { useAuth } from '../../hooks/auth/useAuth'

export const SignUp: React.FC = () => {
	const data: BaseInputJSON[] = formDataLogin.register
	const { signUp } = useAuth()

	const handleSubmit = (event: React.FormEvent<HTMLFormElement>): void => {
		const data = handleFields(event)

		const user: authSignUp = {
			name: data.name as string,
			email: data.email as string,
			password: data.password as string,
			confirmPassword: data.confirmPassword as string,
			role: Number(data.role)
		}
		console.log(user)
		signUp(user)
	}

	return (
		<Box
			sx={{
				display: 'flex',
				flexDirection: 'column',
				maxWidth: '300px',
				margin: 'auto',
				backgroundColor: 'white',
				color: 'black',
				padding: '16px',
				borderRadius: '8px',
				marginTop: '3rem',
				boxShadow: '0 2px 4px rgba(0, 0, 0, 0.6)'
			}}
		>
			<Typography variant='h5'>Registro</Typography>
			<Form handleSubmit={handleSubmit} data={data} value={'Registro'} />
		</Box>
	)
}
