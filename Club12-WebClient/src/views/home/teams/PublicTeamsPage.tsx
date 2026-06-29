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

export default function PublicTeamsPage() {
  const navigate = useNavigate();
  const { teams, getTeamsByFiltered } = useTeam();
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(false);

  const fetchTeams = useCallback(
    async (name?: string) => {
      setLoading(true);
      await getTeamsByFiltered({ name, pageSize: 100, pageNumber: 1 });
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
    }, 600);
    return () => clearTimeout(timeout);
  }, [search, fetchTeams]);

  return (
    <Container maxWidth="lg" sx={{ py: 5 }}>
      <Typography variant="h4" fontWeight="bold" mb={1}>
        Equipos
      </Typography>
      <Typography variant="body1" color="text.secondary" mb={3}>
        Todos los equipos que participan en la liga.
      </Typography>

      <TextField
        placeholder="Buscar equipo..."
        size="small"
        value={search}
        onChange={e => setSearch(e.target.value)}
        sx={{ mb: 4, maxWidth: 320 }}
        InputProps={{
          startAdornment: (
            <InputAdornment position="start">
              <SearchIcon fontSize="small" />
            </InputAdornment>
          ),
        }}
      />

      {loading ? (
        <Box display="flex" justifyContent="center" py={6}>
          <CircularProgress />
        </Box>
      ) : !teams || teams.length === 0 ? (
        <Typography color="text.secondary">No hay equipos disponibles.</Typography>
      ) : (
        <Grid container spacing={3}>
          {teams.map(team => (
            <Grid item xs={12} sm={6} md={4} key={team.id}>
              <Card sx={{ height: '100%' }}>
                <CardActionArea
                  onClick={() => navigate(`/equipos/${team.id}`)}
                  sx={{ height: '100%' }}
                >
                  <CardContent>
                    <Box display="flex" alignItems="center" gap={2} mb={1.5}>
                      <TeamLogo teamName={team.name} logoUrl={team.logoUrl} size={48} />
                      <Box>
                        <Typography variant="h6" lineHeight={1.2}>
                          {team.name}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {team.threeLetterCode}
                        </Typography>
                      </Box>
                    </Box>
                    {team.shirtColor && (
                      <Typography variant="body2" color="text.secondary">
                        Camiseta: {team.shirtColor}
                      </Typography>
                    )}
                    <Typography variant="body2" color="text.secondary">
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
