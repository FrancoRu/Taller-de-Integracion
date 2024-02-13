import Login from './components/Login'
import { ThemeProvider } from '@emotion/react'
import './App.css'
import theme from './styles/theme'
import { Routes, Route, BrowserRouter as Router } from 'react-router-dom'
import Home from './components/Home'
import { CssBaseline } from '@mui/material/'
import TournamentView from './components/TournamentView'

function App (): JSX.Element {
  return (
    <>
    <ThemeProvider theme={theme}>
    <CssBaseline/>
      <Router>
        <div style={{ flex: 1 }}>
          <Routes>
              <Route path="/home" element={<Home/>} />
              <Route path="/login" element={<Login />} />
              <Route path="/torneo" element={<TournamentView/>} />

          </Routes>
        </div>
        </Router>
    </ThemeProvider>
    </>
  )
}

export default App
