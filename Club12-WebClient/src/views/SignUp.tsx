import { BaseInputJSON } from "../types/forms/form"
import formData from "../data/readJSON.json"
import { MyForm } from "../components/form/form"
import { FormEvent } from "react"
import '../styles/auth/auth.css'
import { Box, Typography } from "@mui/material"


 const SignUp = () => {
    const data: BaseInputJSON[]|undefined = formData.register
    const handleChange = (event: FormEvent<HTMLFormElement>):void =>{} 
    return (<div id='container'>
        <div id='blur'>
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
      <Typography variant="h5">Registrar</Typography>
            <MyForm data={data} value="Register" handleSubmit={handleChange} />
    </Box>
        </div>
    </div>)
}

export default SignUp