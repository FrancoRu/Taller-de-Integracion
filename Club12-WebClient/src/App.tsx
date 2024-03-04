import '@fontsource/roboto/300.css'
import '@fontsource/roboto/400.css'
import '@fontsource/roboto/500.css'
import '@fontsource/roboto/700.css'

import { Route, Routes } from 'react-router-dom'

import { ThemeProvider } from '@emotion/react'
import { CssBaseline } from '@mui/material/'
import theme from './styles/theme'
import './styles/main.css'
import { SignIn } from './views/access/SignIn'
import { SignUp } from './views/access/SignUp'
// import SignUp from './views/SignUp'
// import { SignIn } from './views/access/signIn'

function App() {
	return (
		<ThemeProvider theme={theme}>
			<CssBaseline />
			<Routes>
				<Route path='/' element={<SignIn />} />
				<Route path='/register' element={<SignUp />} />
			</Routes>
		</ThemeProvider>
	)
}

export default App
