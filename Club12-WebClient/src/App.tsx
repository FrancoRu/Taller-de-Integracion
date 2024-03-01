import '@fontsource/roboto/300.css'
import '@fontsource/roboto/400.css'
import '@fontsource/roboto/500.css'
import '@fontsource/roboto/700.css'

import { Route, Routes } from 'react-router-dom'
import ProtectedRoute from './pages/ProtectedRoute'
import { NavBarPage } from './pages/navbar/NavbarPage'

function App() {
	return (
		<>
			<NavBarPage />
			<Routes>
				<Route path="/" element={<h1>Home</h1>}></Route>
				<Route path="/login" element={<h1>Hello wordl!</h1>}></Route>
				<Route element={<ProtectedRoute />}>
					<Route path="/home" element={<></>} />
					<Route path="/home" element={<></>} />
					<Route path="/home" element={<></>} />
					<Route path="/home" element={<></>} />
				</Route>
			</Routes>
		</>
	)
}

export default App
