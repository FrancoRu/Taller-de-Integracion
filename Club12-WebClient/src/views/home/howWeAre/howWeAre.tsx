import { Container, Typography, Box } from "@mui/material";

export default function HowWeAre() {
    

    return (
        <Container maxWidth="md" sx={{ py: 5 }}>
            <Box
                sx={{
                    textAlign: "left",
                    mb: 4
                }}>
                <Typography variant="h3" component="h1" gutterBottom sx={{
                    fontWeight: "bold"
                }}>
                    ¿Quiénes Somos?
                </Typography>
            </Box>
            <Box>
                <Typography sx={{
                    marginBottom: "16px"
                }}>
                    La Liga de Basquet se llama Club 12 “La Vuelta” está pensada para todos aquellos jugadores, ex – jugadores y aficionados al básquet que quieran compartir un momento de ocio y volver a disfrutar de un partido completo con todas las características y exigencias que presenta cada partido.
                </Typography>
                <Typography sx={{
                    marginBottom: "16px"
                }}>
                    La idea es que además de beneficiarse con lo nombrado anteriormente, cada jugador pueda compartir con sus pares un momento de distracción como así también poder conocer gente nueva y reencontrarse con ex compañeros del club o de las categorías de las cuales alguna vez formaron parte.
                </Typography>
                <Typography sx={{
                    marginBottom: "16px"
                }}>
                    La Liga de Basquet Club 12 “La Vuelta” es la realización, armonización, confraternización desinteresada y leal, y principalmente el acercamiento entre quienes integran distintos equipos y por consiguiente sabedores de las responsabilidades y actitudes que van a desarrollar hechos que deberán tener en consideración quienes pretendan adoptar posturas antideportivas e incitaciones provocativas que tiendan a engendrar la violencia dentro y/o fuera del campo deportivo los que serán severamente sancionados para sostener incólumes los principios fundamentales de quienes fueron los hacedores de esta liga y mantener en vigencia el contenido esencial de una amistad cosechada y duradera, más una continuidad deportiva sana.
                </Typography>
                <Typography sx={{
                    marginBottom: "16px"
                }}>
                    Es una liga que va creciendo con cada temporada, empezando con 10 equipos en la 1ra, 15 equipos en la 2da segunda y 19 al día de hoy para comenzar la 3ra temporada.
                </Typography>
                <Typography sx={{
                    marginBottom: "16px"
                }}>
                    Se juega todos los domingos en el Club Olimpia y en el Club Ciclista con casi 250 jugadores que se mueven todos los domingos en ambos clubes.
                </Typography>
            </Box>
        </Container>
    );
}
