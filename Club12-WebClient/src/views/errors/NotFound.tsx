import { Typography, Box, Button } from '@mui/material';
import { Link } from 'react-router-dom';
import { orange, grey } from '@mui/material/colors';

export default function NotFound(){
  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        height: '100vh',
        textAlign: 'center',
        backgroundColor: grey[100],
        padding: 3,
      }}
    >
      <Typography variant="h2" sx={{ color: orange[500], fontWeight: 'bold' }}>
        404 - Out of Bounds!
      </Typography>
      <Typography variant="h5" sx={{ marginTop: 2 }}>
        Looks like you’ve thrown the ball out of bounds.
      </Typography>
      <Typography variant="body1" sx={{ marginTop: 2, color: grey[700] }}>
        The page you're looking for doesn't exist. How about we get back to the
        court?
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
          Back to Home
        </Button>
      </Box>
    </Box>
  );
}
