import React from 'react'
import { TextField, Button, Box, Typography } from '@mui/material'
import { useFormik } from 'formik'
import * as yup from 'yup'
import useApiRequest from '../hooks/useApiRequest'

interface LoginValues {
  username: string
  password: string
}

const Login: React.FC = () => {
  const { sendRequest } = useApiRequest()

  const initialValues: LoginValues = {
    username: '',
    password: ''
  }

  const validationSchema = yup.object({
    username: yup.string().required('Username is required'),
    password: yup.string().required('Password is required')
  })

  const {
    handleSubmit,
    errors,
    values,
    handleChange
  } = useFormik<LoginValues>({
    initialValues,
    validationSchema,
    onSubmit: (values) => {
      alert(JSON.stringify(values, null, 2))

      const url = 'https://jsonplaceholder.typicode.com/posts'

      sendRequest(url, values).then((res) => {
        console.log('res', res)
      })
    }
  })

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
      <Typography variant="h5">Login</Typography>
      <form onSubmit={handleSubmit} id={'login-form'}>
        <TextField
          label="User"
          variant="outlined"
          margin="normal"
          name="username"
          value={values.username}
          error={!!errors.username}
          onChange={handleChange}
          color="primary"
          sx={{ color: '#ffffff' }}
        />
        <TextField
          label="Password"
          type="password"
          variant="outlined"
          margin="normal"
          name="password"
          value={values.password}
          error={!!errors.password}
          onChange={handleChange}
          color="primary"
          sx={{ color: '#ffffff' }}
        />
        <Button
          variant="contained"
          color="primary"
          type="submit"
          sx={{ marginTop: '16px', color: 'white' }}
        >
          Iniciar sesión
        </Button>
      </form>
    </Box>
  )
}

export default Login
