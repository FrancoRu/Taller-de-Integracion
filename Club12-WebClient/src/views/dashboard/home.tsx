import { Typography, Paper } from '@mui/material'
import { useAuth } from '../../hooks/auth/useAuth'


export const Home = () => {
  const { isAuthenticated, user } = useAuth()

  return (
    <Paper elevation={3} sx={{ p: 3 }}>
      <Typography variant="h4" gutterBottom>
        {isAuthenticated 
          ? `Bienvenido, ${user?.userName}!`
          : 'Bienvenido a nuestro Dashboard'}
      </Typography>
      <Typography variant="body1">
        {isAuthenticated 
          ? 'Aquí encontrarás toda la información que necesitas.'
          : 'Por favor, inicia sesión para acceder a todas las funcionalidades.'}
      </Typography>
    </Paper>
  )
}