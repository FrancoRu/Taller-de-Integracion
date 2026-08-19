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
import TeamLogo from '@/views/core/components/TeamLogo';
import { GUID } from '@/modules/core/types/types';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';

export default function PublicTeamPage() {
  const { teamId } = useParams<{ teamId: GUID }>();
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

  if (!team || team.id !== teamId) {
    return (
      <Container maxWidth="md" sx={{ py: 5 }}>
        <Typography variant="h5" component="h1" sx={{
          mb: 2
        }}>Equipo no encontrado</Typography>
        <Button onClick={() => navigate(APP_ROUTES.publicTeams)}>Volver a equipos</Button>
      </Container>
    );
  }

  return (
    <Container maxWidth="md" sx={{ py: 5 }}>
      <Button
        onClick={() => navigate(APP_ROUTES.publicTeams)}
        sx={{ mb: 3, pl: 0 }}
        color="inherit"
      >
        ← Volver a equipos
      </Button>

      <Box
        sx={{
          display: "flex",
          alignItems: "center",
          gap: 3,
          mb: 3
        }}>
        <TeamLogo teamName={team.name} logoUrl={team.logoUrl} size={72} />
        <Box>
          <Typography variant="h4" component="h1" sx={{
            fontWeight: "bold"
          }}>
            {team.name}
          </Typography>
          <Typography variant="subtitle1" component="p" sx={{
            color: "text.secondary"
          }}>
            {team.threeLetterCode}
          </Typography>
        </Box>
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
            Color de camiseta
          </Typography>
          <Typography>{team.shirtColor || '—'}</Typography>
        </Grid>
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
          {team.players.map((player, index) => (
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
                  <Box
                    sx={{
                      width: 32,
                      height: 32,
                      borderRadius: '50%',
                      bgcolor: 'secondary.main',
                      color: '#fff',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      flexShrink: 0,
                      fontSize: '0.8rem',
                      fontWeight: 700,
                    }}
                  >
                    {index + 1}
                  </Box>
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
