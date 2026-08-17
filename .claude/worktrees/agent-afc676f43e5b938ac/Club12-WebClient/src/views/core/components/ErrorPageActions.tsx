import { Box, Button } from '@mui/material';
import { Link } from 'react-router-dom';
import theme from '@/theme';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';

interface ErrorPageActionsProps {
  showLogin?: boolean;
}

const ErrorPageActions: React.FC<ErrorPageActionsProps> = ({
  showLogin = false,
}) => (
  <Box sx={{ mt: 4, display: 'flex', gap: 2 }}>
    <Button
      component={Link}
      to={APP_ROUTES.home}
      variant="outlined"
      sx={{
        borderColor: theme.palette.primary.main,
        color: theme.palette.primary.main,
        '&:hover': {
          borderColor: '#d45a00',
          backgroundColor: '#d45a00',
          color: '#fff',
        },
      }}
    >
      Ir al inicio
    </Button>
    {showLogin && (
      <Button
        component={Link}
        to={APP_ROUTES.login}
        variant="outlined"
        sx={{
          borderColor: theme.palette.primary.main,
          color: theme.palette.primary.main,
          '&:hover': {
            borderColor: '#d45a00',
            backgroundColor: '#d45a00',
            color: '#fff',
          },
        }}
      >
        Ir al login
      </Button>
    )}
  </Box>
);

export default ErrorPageActions;
