import React from 'react'
import ReactDOM from 'react-dom'
import App from './App'
import './index.css'
import { BrowserRouter } from 'react-router-dom'
<<<<<<< HEAD
import { MyForm } from './components/form/form'
ReactDOM.createRoot(document.getElementById('root')!).render(
	<React.StrictMode>
		{/* // <BrowserRouter> */}
		<App />
		{/* // </BrowserRouter> */}
	</React.StrictMode>
=======

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <BrowserRouter>
      <App />
    </BrowserRouter>
  </React.StrictMode>
>>>>>>> d9fb9ed5dde741cf4cabae97290f56c8eaab95b3
)
