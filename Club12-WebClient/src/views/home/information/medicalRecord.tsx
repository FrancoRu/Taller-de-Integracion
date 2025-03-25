import { Container, Typography, Box } from '@mui/material';

export default function MedicalRecord() {
  return (
    <Container
      maxWidth="md"
      sx={{
        py: 5,
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
      }}
    >
      <Box width="100%" maxWidth="lg" sx={{ p: 4 }}>
        <Typography variant="h3" fontWeight="bold" gutterBottom>
          Ficha Médica
        </Typography>
        <Typography>
          LA FICHA MÉDICA DEBE SER DESCARGADA Y LUEGO, UNA VEZ QUE ESTÉ COMPLETA
          POR EL MÉDICO, DEBE SER ENTREGADA A LA ORGANIZACIÓN DE CLUB12.
        </Typography>
      </Box>
    </Container>
  );
}
