import { Container, Typography, Box } from '@mui/material';

const paragraphs = [
  'La Liga de Básquet Club 12 "La Vuelta" está pensada para todos aquellos jugadores, ex jugadores y aficionados al básquet que quieran compartir un momento de ocio y volver a disfrutar de un partido completo, con todas las características y exigencias que presenta cada partido.',
  'Más allá de lo deportivo, la idea es que cada jugador pueda compartir con sus pares un momento de distracción, conocer gente nueva y reencontrarse con ex compañeros del club o de las categorías de las cuales alguna vez formaron parte.',
  'Club 12 "La Vuelta" es la realización, armonización y confraternización desinteresada y leal entre quienes integran distintos equipos, sabedores de las responsabilidades y actitudes que deben tener en consideración quienes pretendan adoptar posturas antideportivas dentro y/o fuera del campo. El objetivo es sostener los principios de quienes fueron los hacedores de esta liga y mantener en vigencia una amistad cosechada y duradera, junto con una continuidad deportiva sana.',
];

export default function HowWeAre() {
  return (
    <Box sx={{ bgcolor: 'secondary.main', color: '#fff', py: { xs: 6, md: 8 } }}>
      <Container maxWidth="md">
        <Typography
          variant="h3"
          component="h1"
          sx={{ fontWeight: 700, mb: 2 }}
        >
          ¿Quiénes somos?
        </Typography>
        <Typography
          sx={{
            fontSize: '1.15rem',
            color: 'rgba(255,255,255,0.85)',
            mb: 5,
            maxWidth: 640,
          }}
        >
          Una liga de básquet amateur hecha por y para quienes aman este
          deporte: competencia, compañerismo y buena onda dentro y fuera de
          la cancha.
        </Typography>
      </Container>

      <Box sx={{ bgcolor: 'background.default', color: 'text.primary', pt: { xs: 5, md: 6 } }}>
        <Container maxWidth="md">
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
            {paragraphs.map((text, index) => (
              <Typography key={index} sx={{ lineHeight: 1.9, fontSize: '1.05rem' }}>
                {text}
              </Typography>
            ))}
          </Box>
        </Container>
      </Box>
    </Box>
  );
}
