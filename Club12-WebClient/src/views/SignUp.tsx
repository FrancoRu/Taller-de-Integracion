import formData from '../data/readJSON.json'
import { Form } from '../components/form/form'
import { FormEvent } from 'react'
import '../styles/auth/auth.css'
import { Box, Typography } from '@mui/material'
import { BaseInputJSON } from '../types/form/form'

const SignUp = () => {
	const data: BaseInputJSON[] | undefined = formData.register
	const handleChange = (event: FormEvent<HTMLFormElement>): void => {}
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
			<Typography variant='h5'>Registrar</Typography>
			<Form data={data} value='Register' handleSubmit={handleChange} />
		</Box>
	)
}

export default SignUp
