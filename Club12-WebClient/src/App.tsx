import '@fontsource/roboto/300.css';
import '@fontsource/roboto/400.css';
import '@fontsource/roboto/500.css';
import '@fontsource/roboto/700.css';
import { redirect, Route, Routes } from 'react-router-dom';
import Home from './views/home/home';
import Login from './views/auth/login';
import HowWeAre from './views/home/howWeAre/howWeAre';
import NavMenu from './views/home/navMenu';
import { useAuth } from './modules/auth/hook/auth.hook';
import { useEffect } from 'react';
import MedicalRecord from './views/home/information/medicalRecord';
import Regulation from './views/home/information/regulation';

function App() {
  const { isAuthenticated } = useAuth();

  useEffect(() => {
    redirect('/');
  }, [isAuthenticated]);

  return (
    <div>
      <NavMenu />
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/quienes-somos" element={<HowWeAre />} />
        <Route path="/ficha-medica" element={<MedicalRecord />} />
        <Route path="/reglamento" element={<Regulation />} />
        <Route path="/login" element={<Login />} />
      </Routes>
    </div>
  );
}

export default App;
