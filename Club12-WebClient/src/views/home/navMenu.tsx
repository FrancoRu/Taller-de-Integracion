import { Link } from 'react-router-dom';
import { AppBar, Toolbar, Typography, Button, Box } from '@mui/material';
import { orange, grey } from '@mui/material/colors';

interface NavMenuProps {
  isAuthenticated: boolean;
  onLogout: () => void;
}

const NavMenu: React.FC<NavMenuProps> = ({ isAuthenticated, onLogout }) => {
  return (
    <AppBar position="sticky" sx={{ backgroundColor: orange[500] }}>
      <Toolbar sx={{ justifyContent: 'space-between' }}>
        <Typography variant="h6" sx={{ fontWeight: 'bold', color: grey[900] }}>
          CLUB12 - APP
        </Typography>
        <Box sx={{ display: 'flex' }}>
          <Button
            component={Link}
            to="/"
            sx={{
              color: grey[900],
              textDecoration: 'none',
              marginRight: 2,
              fontWeight: 'bold',
              '&:hover': {
                color: grey[50],
              },
            }}
          >
            Inicio
          </Button>
          <Button
            component={Link}
            to="/quienes-somos"
            sx={{
              color: grey[900],
              textDecoration: 'none',
              marginRight: 2,
              fontWeight: 'bold',
              '&:hover': {
                color: grey[50],
              },
            }}
          >
            Quienes Somos
          </Button>
          <Button
            component={Link}
            to="/informacion"
            sx={{
              color: grey[900],
              textDecoration: 'none',
              marginRight: 2,
              fontWeight: 'bold',
              '&:hover': {
                color: grey[50],
              },
            }}
          >
            Información
          </Button>
        </Box>
        <Box>
          {isAuthenticated ? (
            <>
              <Typography
                variant="body1"
                sx={{ color: grey[900], marginRight: 2 }}
              >
                Bienvenido, authenticated user!
              </Typography>
              <Button
                onClick={onLogout}
                sx={{
                  color: grey[900],
                  fontWeight: 'bold',
                  '&:hover': {
                    backgroundColor: grey[300],
                  },
                }}
              >
                Log Out
              </Button>
            </>
          ) : (
            <Button
              component={Link}
              to="/login"
              sx={{
                color: grey[900],
                fontWeight: 'bold',
                '&:hover': {
                  backgroundColor: grey[300],
                },
              }}
            >
              Log In
            </Button>
          )}
        </Box>
      </Toolbar>
    </AppBar>
  );
};

export default NavMenu;
