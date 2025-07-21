import React from 'react';
import ReactDOM from 'react-dom/client';
import './index.css';
import App from './App';
import { AuthProvider } from './modules/auth/context/auth.context';
import { BrowserRouter } from 'react-router-dom';
import { ErrorProvider } from './modules/error/context/error.context';
import { ThemeProvider } from '@emotion/react';
import theme from './theme';

//process.loadEnvFile();
ReactDOM.createRoot(document.getElementById('root') as HTMLElement).render(
  <React.StrictMode>
    <ThemeProvider theme={theme}>
      <BrowserRouter>
        <ErrorProvider>
          <AuthProvider>
            <App />
          </AuthProvider>
        </ErrorProvider>
      </BrowserRouter>
    </ThemeProvider>
  </React.StrictMode>
);
