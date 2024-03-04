import React from 'react'
import ReactDOM from 'react-dom/client'
import './index.css'
import App from './App'
import { AuthProvider } from './context/auth/auth.context'
import { ErrorProvider } from './context/error/error.context'
import { BrowserRouter } from 'react-router-dom'

ReactDOM.createRoot(document.getElementById('root') as HTMLElement).render(
	<React.StrictMode>
		<BrowserRouter>
			<ErrorProvider>
				<AuthProvider>
					<App />
				</AuthProvider>
			</ErrorProvider>
		</BrowserRouter>
	</React.StrictMode>
)
