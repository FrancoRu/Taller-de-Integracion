import "@fontsource/roboto/300.css"
import "@fontsource/roboto/400.css"
import "@fontsource/roboto/500.css"
import "@fontsource/roboto/700.css"

import { Navigate, Route, Routes } from "react-router-dom"

import { ThemeProvider } from "@emotion/react"
import { CssBaseline } from "@mui/material/"
import theme from "./styles/theme"
import "./styles/main.css"
import { SignIn } from "./views/access/SignIn"
import { NotFound } from "./views/errors/NotFound"
import { useAuth } from "./hooks/auth/useAuth"
import Layout from "./components/Layout/Layout"
import { Home } from "./views/dashboard/Home"


function App() {
  const { isAuthenticated } = useAuth()

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Layout>
        <Routes>
          <Route path="/" element={<Home />} />
          <Route
            path="/login"
            element={isAuthenticated ? <Navigate to="/" /> : <SignIn />}
          />
          <Route path="*" element={<NotFound />} />
        </Routes>
      </Layout>
    </ThemeProvider>
  )
}

export default App