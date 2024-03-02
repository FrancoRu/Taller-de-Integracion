import React from 'react'
import {  Box, Typography } from '@mui/material'
// import { useFormik } from 'formik'
// import * as yup from 'yup'
// import useApiRequest from '../hooks/useApiRequest'
import { MyForm } from '../components/form/form'
// import '../styles/form/form.css'
import '../styles/auth/auth.css'
// interface LoginValues {
//   username: string
//   password: string
// }

import formDataLogin from '../data/readJSON.json' 
import { BaseInputJSON } from '../types/forms/form'

export const Login: React.FC = () => {
  const data: BaseInputJSON[] = formDataLogin.login
  const handleSubmit = (event: React.FormEvent<HTMLFormElement>):void => {}  

  return (
    <div id='container'><div id='blur'>
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
      <MyForm handleSubmit={handleSubmit} data={data} value={"Ingresar"}/>
    </Box>
    </div>
    </div>
  )
}

{/* <form onSubmit={handleSubmit} id={'login-form'}>
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
</form> */}

// const { sendRequest } = useApiRequest()

//   const initialValues: LoginValues = {
//     username: '',
//     password: ''
//   }

//   const validationSchema = yup.object({
//     username: yup.string().required('Username is required'),
//     password: yup.string().required('Password is required')
//   })

//   const {
//     handleSubmit,
//     errors,
//     values,
//     handleChange
//   } = useFormik<LoginValues>({
//     initialValues,
//     validationSchema,
//     onSubmit: (values) => {
//       alert(JSON.stringify(values, null, 2))

//       const url = 'https://jsonplaceholder.typicode.com/posts'

//       sendRequest(url, values).then((res) => {
//         console.log('res', res)
//       })
//     }
//   })