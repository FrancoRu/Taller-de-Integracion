import { Link, useRouteError } from 'react-router-dom';
import { Typography, Box, Button } from '@mui/material';
import { orange, grey } from '@mui/material/colors';

interface ErrorDetails {
  statusText?: string;
  message: string;
}

export default function ErrorPage() {
  const error = useRouteError() as ErrorDetails | { message: string };

  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        height: '100vh',
        textAlign: 'center',
        backgroundColor: grey[200],
        padding: 3,
      }}
    >
      <Typography variant="h2" sx={{ color: orange[500], fontWeight: 'bold' }}>
        Oops!
      </Typography>
      <Typography variant="h5" sx={{ marginTop: 2, fontStyle: 'italic' }}>
        Sorry, an unexpected error has occurred.
      </Typography>
      <Typography variant="body1" sx={{ marginTop: 2, color: grey[800] }}>
        <i>
          {(error as ErrorDetails).statusText ||
            (error as ErrorDetails).message}
        </i>
      </Typography>
      <Box sx={{ marginTop: 4 }}>
        <Button
          component={Link}
          to="/"
          variant="contained"
          sx={{
            backgroundColor: orange[500],
            color: grey[900],
            '&:hover': {
              backgroundColor: orange[700],
            },
          }}
        >
          Go Back to Home
        </Button>
      </Box>
    </Box>
  );
}
