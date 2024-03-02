import "@fontsource/roboto/300.css"
import "@fontsource/roboto/400.css"
import "@fontsource/roboto/500.css"
import "@fontsource/roboto/700.css"

import { Route, Routes } from "react-router-dom"

import { ThemeProvider } from '@emotion/react'
import { CssBaseline } from '@mui/material/'
import theme from './styles/theme'
import './styles/main.css'
import { Login } from "./views/Login"
import SignUp from "./views/signUp"

function App () {
  // const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
  //   // Evita que el formulario se envíe de manera predeterminada
  //   event.preventDefault()
  //   const fields = Object.fromEntries(new window.FormData(event.currentTarget))
  //   console.log(fields)
  // }
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline/>
      <Routes>
        <Route path="/" element={<Login />} />
        <Route path="/register" element={<SignUp />} />
      </Routes>
    </ThemeProvider>
  )
}

export default App
