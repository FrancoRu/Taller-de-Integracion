import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {  Box,  TextField,  Button,  Typography,  Card,  CardContent,  useTheme,} from '@mui/material';

export default function Login() {
  const theme = useTheme();
  const navigate = useNavigate();
  const [credentials, setCredentials] = useState({
    username: '',
    password: '',
  });
  const [error, setError] = useState('');

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setCredentials({ ...credentials, [e.target.name]: e.target.value });
  };

  const handleLogin = () => {
    if (
      credentials.username === 'admin' &&
      credentials.password === 'password'
    ) {
      navigate('/');
    } else {
      setError('Usuario o contraseña incorrectos');
    }
  };

  return (
    <Box
      display="flex"
      justifyContent="center"
      alignItems="center"
      minHeight="90vh"
      sx={{ backgroundColor: theme.palette.background.default }}
    >
      <Card sx={{ maxWidth: 400, padding: 3, boxShadow: theme.shadows[5] }}>
        <CardContent>
          <Typography
            variant="h4"
            gutterBottom
            align="center"
            color={theme.palette.primary.main}
          >
            Administrador
          </Typography>
          {error && (
            <Typography
              color="error"
              variant="body2"
              align="center"
              gutterBottom
            >
              {error}
            </Typography>
          )}
          <TextField
            fullWidth
            label="Usuario"
            name="username"
            variant="outlined"
            margin="normal"
            value={credentials.username}
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
          />
          <Button
            fullWidth
            variant="contained"
            color="primary"
            onClick={handleLogin}
            sx={{ mt: 2, color: 'white', fontWeight: 'bold' }}
          >
            Iniciar Sesion
          </Button>
        </CardContent>
      </Card>
    </Box>
  );
}
