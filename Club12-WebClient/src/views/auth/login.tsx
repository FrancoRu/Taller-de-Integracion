import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  TextField,
  Button,
  Link,
  Typography,
  Card,
  CardContent,
  useTheme,
} from '@mui/material';
import { useAuth } from '@/modules/auth/hook/auth.hook';
import { LogInUserRequest } from '@/modules/auth/type/auth';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';

export default function Login() {
  const theme = useTheme();
  const navigate = useNavigate();
  const { signIn } = useAuth();
  const [credentials, setCredentials] = useState<LogInUserRequest>({
    email: '',
    password: '',
  });

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setCredentials({ ...credentials, [e.target.name]: e.target.value });
  };

  const handleLogin = async () => {
    // A failed sign-in already shows the standard Spanish toast (setMessage
    // inside signIn) — nothing to do here beyond navigating on success.
    const success = await signIn(credentials);
    if (success) {
      navigate(APP_ROUTES.panel);
    }
  };

  return (
    <Box
      sx={{
        display: "flex",
        justifyContent: "center",
        alignItems: "center",
        minHeight: "90vh",
        backgroundColor: theme.palette.background.default
      }}>
      <Card sx={{ maxWidth: 400 }}>
        <CardContent>
          <Typography
            variant="h4"
            gutterBottom
            align="center"
            color={theme.palette.primary.main}
          >
            Administrador
          </Typography>
          <TextField
            fullWidth
            label="Usuario"
            name="email"
            variant="outlined"
            margin="normal"
            value={credentials.email}
            onChange={handleChange}
          />
          <TextField
            fullWidth
            label="Contraseña"
            name="password"
            type="password"
            variant="outlined"
            margin="normal"
            value={credentials.password}
            onChange={handleChange}
            onKeyDown={(e) => {
              if (e.key === 'Enter') {
                void handleLogin();
              }
            }}
          />
          <Button
            fullWidth
            variant="contained"
            color="primary"
            onClick={handleLogin}
          >
            Iniciar Sesión
          </Button>
          <Box sx={{ mt: 2, textAlign: 'center' }}>
            <Link
              component="button"
              type="button"
              variant="body2"
              onClick={() => navigate(APP_ROUTES.forgotPassword)}
            >
              ¿Olvidaste tu contraseña?
            </Link>
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
}
