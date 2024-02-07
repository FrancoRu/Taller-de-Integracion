import Login from './components/Login'
import { ThemeProvider } from '@emotion/react'
import './App.css'
import theme from './styles/theme'
import { Routes, Route, BrowserRouter as Router } from 'react-router-dom'
import Nav from './layouts/nav'
import Home from './components/Home'
import { CssBaseline } from '@mui/material/'

function App (): JSX.Element {
  return (
    <>
    <ThemeProvider theme={theme}>
    <CssBaseline/>
      <Router>
        <div style={{ flex: 1 }}>
          <Routes>
            <Route path="/" element={<Nav />}>
              <Route index element={<Home/>} />
              <Route path="login" element={<Login />} />
            </Route>
          </Routes>
        </div>
        </Router>
    </ThemeProvider>
    </>
  )
}

export default App
