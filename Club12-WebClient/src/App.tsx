import '@fontsource/roboto/300.css'
import '@fontsource/roboto/400.css'
import '@fontsource/roboto/500.css'
import '@fontsource/roboto/700.css'
import { Route, Routes } from 'react-router-dom'
import { Login } from './views/auth/login'
import { Home } from './views/home/home'

// import { Login } from "./views/auth/login";
// import { Routes } from "./modules/core/types/types";

function App () {
  return (
    <div>
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/login" element={<Login />} />
      </Routes>
    </div>
  )
}

export default App
