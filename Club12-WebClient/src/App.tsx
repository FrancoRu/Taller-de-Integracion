import '@fontsource/roboto/300.css';
import '@fontsource/roboto/400.css';
import '@fontsource/roboto/500.css';
import '@fontsource/roboto/700.css';
import { Routes, Route } from 'react-router-dom';
import { Suspense } from 'react';
import { routes } from './routes';
import ErrorBoundary from './views/errors/error-boundary';
import Loading from './views/core/loading';
import { useAuth } from './modules/auth/hook/auth.hook';
import NavMenu from './views/home/navMenu';
import theme from './theme';
import { ThemeProvider } from '@mui/material';

function App() {
  const { isAuthenticated, logOut } = useAuth();  

  return (
    <ThemeProvider theme={theme}>
    <ErrorBoundary>
      <NavMenu isAuthenticated={isAuthenticated} onLogout={logOut} />
      <Suspense fallback={<Loading />}>
        <Routes>
          {routes.map((route, index) => (
            <Route key={index} path={route.path} element={route.element} />
          ))}
        </Routes>
      </Suspense>
    </ErrorBoundary>
    </ThemeProvider>
  );
}

export default App;
