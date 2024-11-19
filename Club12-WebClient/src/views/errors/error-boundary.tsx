// ErrorBoundary.tsx
import React, { Component, ErrorInfo } from 'react';
import { Box, Typography, Button } from '@mui/material';
import { Link } from 'react-router-dom';
import { orange, grey } from '@mui/material/colors';

interface ErrorBoundaryState {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
}

class ErrorBoundary extends Component<{ children: React.ReactNode }, ErrorBoundaryState> {
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
    console.error("Error caught by Error Boundary:", error, errorInfo);
    this.setState({
      errorInfo: errorInfo,
    });
  }

  render() {
    if (this.state.hasError) {
      return (
        <Box
          sx={{
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
            justifyContent: "center",
            height: "100vh",
            textAlign: "center",
            backgroundColor: grey[200],
            padding: 3,
          }}
        >
          <Typography variant="h2" sx={{ color: orange[500], fontWeight: "bold" }}>
            Something Went Wrong
          </Typography>
          <Typography variant="h5" sx={{ marginTop: 2, fontStyle: "italic" }}>
            We encountered an unexpected error.
          </Typography>
          <Typography variant="body1" sx={{ marginTop: 2, color: grey[800] }}>
            <i>{this.state.error?.message || "Unknown error occurred."}</i>
          </Typography>
          <Box sx={{ marginTop: 4 }}>
            <Button
              component={Link}
              to="/"
              variant="contained"
              sx={{
                backgroundColor: orange[500],
                color: grey[900],
                "&:hover": {
                  backgroundColor: orange[700],
                },
              }}
            >
              Back to Home
            </Button>
          </Box>
        </Box>
      );
    }

      return this.props.children;
  }
}

export default ErrorBoundary;
