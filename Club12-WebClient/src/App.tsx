import '@fontsource/roboto/300.css'
import '@fontsource/roboto/400.css'
import '@fontsource/roboto/500.css'
import '@fontsource/roboto/700.css'
import './styles/main.css'

import { Route, Routes } from 'react-router-dom'

import { ThemeProvider } from '@emotion/react'
import { CssBaseline } from '@mui/material/'
import theme from './styles/theme'
import { NavBarPage } from './views/navbar/NavbarPage'
import { Home } from './views/home/home'
import { Routes as r } from './types/types.d'
function App() {
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <div id='container'>
        <div id='blur'>
          <NavBarPage />

          <Routes>
            <Route path={r.HOME} element={<Home />} />
            <Route path={r.CAMPEONATO} element={<Home />} />
            <Route path={r.FEMENINO} element={<Home />} />
            <Route path={r.LA_PREVIA} element={<Home />} />
            <Route path={r.COPA_12} element={<Home />} />
          </Routes>
        </div>
      </div>
    </ThemeProvider>
  )
}

export default App
