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
      <Box
        sx={{
          width: "100%",
          maxWidth: "lg",
          p: 4
        }}>
        <Typography variant="h3" component="h1" gutterBottom sx={{
          fontWeight: "bold"
        }}>
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
