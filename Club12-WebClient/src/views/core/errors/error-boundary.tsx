import React, { Component, ErrorInfo } from 'react';
import { Box, Typography, Button } from '@mui/material';

interface ErrorBoundaryState {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
}

class ErrorBoundary extends Component<
  { children: React.ReactNode },
  ErrorBoundaryState
> {
  constructor(props: { children: React.ReactNode }) {
    super(props);
    this.state = {
      hasError: false,
      error: null,
      errorInfo: null,
    };
  }

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { hasError: true, error: error, errorInfo: null };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
    console.error('Error caught by Error Boundary:', error, errorInfo);
    this.setState({
      errorInfo: errorInfo,
    });
  }

  render() {
    if (this.state.hasError) {
      return (
        <Box
          sx={{
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'center',
            height: '100vh',
            textAlign: 'center',
            backgroundColor: 'background.default',
            padding: 3,
          }}
        >
          <Typography
            variant="h2"
            sx={{ color: 'primary.main', fontWeight: 'bold' }}
          >
            Algo salió mal
          </Typography>
          <Typography variant="h5" sx={{ marginTop: 2, fontStyle: 'italic', color: 'text.primary' }}>
            Ocurrió un error inesperado.
          </Typography>
          <Typography variant="body1" sx={{ marginTop: 2, color: 'text.secondary' }}>
            <i>{this.state.error?.message || 'Error desconocido.'}</i>
          </Typography>
          <Box sx={{ marginTop: 4 }}>
            {/* A plain anchor, not react-router's <Link>: this boundary sits
                ABOVE <BrowserRouter> in the provider tree (it must catch
                errors thrown by the router itself), so a <Link> here would
                render with no router context and crash. A full page load
                back to "/" is also the safer recovery from an unknown
                render error anyway — it guarantees fresh app state instead
                of a client-side route change that keeps the broken tree. */}
            <Button href="/" variant="contained" color="primary">
              Volver al inicio
            </Button>
          </Box>
        </Box>
      );
    }

    return this.props.children;
  }
}

export default ErrorBoundary;
