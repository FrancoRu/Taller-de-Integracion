import { Typography, Box, Button } from '@mui/material'
import { Link } from 'react-router-dom'

export const NotFound = () => {
  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        height: '100%',
        textAlign: 'center',
      }}
    >
      <Typography variant="h2" gutterBottom>
        404 - Fuera de la cancha!
      </Typography>
      <Typography variant="h5" gutterBottom>
        Parece que has lanzado el balón fuera de los límites.
      </Typography>
      <Typography variant="body1" paragraph>
        La página que buscas no existe. ¿Qué tal si volvemos a la cancha principal?
      </Typography>
      <Button component={Link} to="/" variant="contained" color="primary">
        Volver al inicio
      </Button>
    </Box>
  )
}