import { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Card,
  CardActionArea,
  CardContent,
  CircularProgress,
  Container,
  Grid,
  InputAdornment,
  TextField,
  Typography,
} from '@mui/material';
import { useTeam } from '@/modules/team/hook/team.hook';
import TeamLogo from '@/views/core/components/TeamLogo';
import { SearchIcon } from '@/views/core/MUI/icons/icons';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { PUBLIC_SEARCH_DEBOUNCE_DELAY_MS } from '@/modules/core/constants/constants';
import { PUBLIC_LISTING_PAGE_SIZE } from '@/modules/core/constants/pagination';

export default function PublicTeamsPage() {
  const navigate = useNavigate();
  const { teams, getTeamsByFiltered } = useTeam();
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(false);

  const fetchTeams = useCallback(
    async (name?: string) => {
      setLoading(true);
      await getTeamsByFiltered({
        name,
        pageSize: PUBLIC_LISTING_PAGE_SIZE,
        pageNumber: 1,
      });
      setLoading(false);
    },
    [getTeamsByFiltered]
  );

  useEffect(() => {
    void fetchTeams();
  }, [fetchTeams]);

  useEffect(() => {
    const timeout = setTimeout(() => {
      void fetchTeams(search || undefined);
    }, PUBLIC_SEARCH_DEBOUNCE_DELAY_MS);
    return () => clearTimeout(timeout);
  }, [search, fetchTeams]);

  return (
    <Container maxWidth="lg" sx={{ py: 5 }}>
      <Typography
        variant="h4"
        component="h1"
        sx={{
          fontWeight: "bold",
          mb: 1
        }}>
        Equipos
      </Typography>
      <Typography
        variant="body1"
        sx={{
          color: "text.secondary",
          mb: 3
        }}>
        Todos los equipos que participan en la liga.
      </Typography>

      <TextField
        placeholder="Buscar equipo..."
        size="small"
        value={search}
        onChange={e => setSearch(e.target.value)}
        sx={{ mb: 4, maxWidth: 320 }}
        slotProps={{
          input: {
            startAdornment: (
              <InputAdornment position="start">
                <SearchIcon fontSize="small" />
              </InputAdornment>
            ),
          }
        }}
      />

      {loading ? (
        <Box
          sx={{
            display: "flex",
            justifyContent: "center",
            py: 6
          }}>
          <CircularProgress />
        </Box>
      ) : !teams || teams.length === 0 ? (
        <Typography sx={{
          color: "text.secondary"
        }}>No hay equipos disponibles.</Typography>
      ) : (
        <Grid container spacing={3}>
          {teams.map(team => (
            <Grid
              key={team.id}
              size={{
                xs: 12,
                sm: 6,
                md: 4
              }}>
              <Card sx={{ height: '100%' }}>
                <CardActionArea
                  onClick={() => navigate(APP_ROUTES.publicTeam.build(team.id))}
                  sx={{ height: '100%' }}
                >
                  <CardContent>
                    <Box
                      sx={{
                        display: "flex",
                        alignItems: "center",
                        gap: 2,
                        mb: 1.5
                      }}>
                      <TeamLogo teamName={team.name} logoUrl={team.logoUrl} size={48} />
                      <Box>
                        <Typography variant="h6" component="h2" sx={{
                          lineHeight: 1.2
                        }}>
                          {team.name}
                        </Typography>
                        <Typography variant="caption" sx={{
                          color: "text.secondary"
                        }}>
                          {team.threeLetterCode}
                        </Typography>
                      </Box>
                    </Box>
                    {team.shirtColor && (
                      <Typography variant="body2" sx={{
                        color: "text.secondary"
                      }}>
                        Camiseta: {team.shirtColor}
                      </Typography>
                    )}
                    <Typography variant="body2" sx={{
                      color: "text.secondary"
                    }}>
                      Jugadores: {team.players?.length ?? 0}
                    </Typography>
                  </CardContent>
                </CardActionArea>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}
    </Container>
  );
}
