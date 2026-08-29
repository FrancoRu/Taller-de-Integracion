import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Box,
  Button,
  CircularProgress,
  Container,
  Divider,
  Grid,
  Paper,
  Stack,
  Typography,
} from '@mui/material';
import { useTeam } from '@/modules/team/hook/team.hook';
import TeamHero from '@/views/core/components/TeamHero';
import JerseySvg from '@/views/core/components/JerseySvg';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';

export default function PublicTeamPage() {
  const { teamId } = useParams<{ teamId: string }>();
  const navigate = useNavigate();
  const { team, getTeamById } = useTeam();
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!teamId) return;
    const fetch = async () => {
      setLoading(true);
      await getTeamById(teamId);
      setLoading(false);
    };
    void fetch();
  }, [teamId, getTeamById]);

  if (loading) {
    return (
      <Box
        sx={{
          display: "flex",
          justifyContent: "center",
          py: 10
        }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!team || (team.id !== teamId && team.slug !== teamId)) {
    return (
      <Container maxWidth="md" sx={{ py: 5 }}>
        <Typography variant="h5" component="h1" sx={{
          mb: 2
        }}>Equipo no encontrado</Typography>
        <Button onClick={() => navigate(APP_ROUTES.publicTournaments)}>Volver a torneos</Button>
      </Container>
    );
  }

  return (
    <Container maxWidth="md" sx={{ py: 5 }}>
      <Button
        onClick={() => navigate(APP_ROUTES.publicTournaments)}
        sx={{ mb: 3, pl: 0 }}
        color="inherit"
      >
        ← Volver a torneos
      </Button>

      <Box sx={{ mb: 3 }}>
        <TeamHero
          name={team.name}
          code={team.threeLetterCode}
          logoUrl={team.logoUrl}
          shirtColor={team.shirtColor}
          secondaryColor={team.shirtSecondaryColor}
          jerseyStyle={team.jerseyStyle}
        />
      </Box>

      <Divider sx={{ mb: 3 }} />

      <Grid container spacing={3} sx={{
        mb: 4
      }}>
        <Grid
          size={{
            xs: 12,
            sm: 4
          }}>
          <Typography variant="subtitle2" component="p" sx={{
            color: "text.secondary"
          }}>
            Jugadores inscriptos
          </Typography>
          <Typography>{team.players?.length ?? 0}</Typography>
        </Grid>
      </Grid>

      <Typography variant="h6" component="h2" sx={{
        mb: 2
      }}>
        Plantel
      </Typography>

      {!team.players || team.players.length === 0 ? (
        <Typography sx={{
          color: "text.secondary"
        }}>
          Este equipo no tiene jugadores registrados.
        </Typography>
      ) : (
        <Grid container spacing={1.5}>
          {team.players.map(player => (
            <Grid
              key={player.id}
              size={{
                xs: 12,
                sm: 6
              }}>
              <Paper
                variant="outlined"
                sx={{ px: 2, py: 1.25 }}
              >
                <Stack direction="row" spacing={2} sx={{
                  alignItems: "center"
                }}>
                  <JerseySvg
                    color={team.shirtColor}
                    secondaryColor={team.shirtSecondaryColor}
                    style={team.jerseyStyle}
                    number={player.jerseyNumber}
                    size={28}
                    title={`Camiseta de ${player.fullName}`}
                  />
                  <Typography variant="body2" noWrap sx={{
                    fontWeight: 500
                  }}>
                    {player.fullName}
                  </Typography>
                </Stack>
              </Paper>
            </Grid>
          ))}
        </Grid>
      )}
    </Container>
  );
}
