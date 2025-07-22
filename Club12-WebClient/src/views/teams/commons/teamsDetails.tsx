import React, { useEffect, useState, useContext } from 'react';
import { useParams } from 'react-router-dom';
import {
  Box,
  Typography,
  Grid,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Card,
  CardContent,
  CardMedia,
  useTheme,
  CircularProgress,
} from '@mui/material';
import { TeamContext } from '@/modules/team/context/team.context';
import { TeamResponse } from '@/modules/team/type/team';

const TeamsDetails: React.FC = () => {
  const theme = useTheme();
  const { teamId } = useParams<{ teamid: GUID }>();

  const teamContext = useContext(TeamContext);

  if (!teamContext) {
    throw new Error('TeamsDetails must be used within a TeamProvider');
  }

  const { getTeamById } = teamContext;

  const [team, setTeam] = useState<TeamResponse | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchTeam = async () => {
      setLoading(true);
      if (teamId) {
        const result = await getTeamById(teamId);
        if (result) {
          setTeam(result);
        }
      }
      setLoading(false);
    };

    fetchTeam();
  }, [teamId, getTeamById]);

  if (loading) {
    return (
      <Box p={3} textAlign="center">
        <CircularProgress />
      </Box>
    );
  }

  if (!team) {
    return (
      <Box p={3} textAlign="center">
        <Typography variant="h6" color="error">
          Team not found!
        </Typography>
      </Box>
    );
  }

  return (
    <Box
      p={3}
      sx={{
        backgroundColor: theme.palette.background.default,
        minHeight: '100vh',
      }}
    >
      <Grid container spacing={3} justifyContent="center">
        <Grid item xs={12} md={8}>
          <Card
            sx={{
              display: 'flex',
              borderRadius: theme.shape.borderRadius,
              boxShadow: theme.shadows[3],
              backgroundColor: theme.palette.background.paper,
            }}
          >
            <CardMedia
              component="img"
              sx={{
                width: 200,
                objectFit: 'cover',
                borderRadius: `${theme.shape.borderRadius}px 0 0 ${theme.shape.borderRadius}px`,
              }}
              image={team.logoUrl || '/placeholder-image.jpg'} // usa una imagen por defecto si no tiene logo
              alt={team.name}
            />
            <CardContent sx={{ flex: 1 }}>
              <Typography
                variant="h4"
                sx={{
                  fontWeight: 'bold',
                  color: theme.palette.primary.main,
                  mb: 1,
                }}
              >
                {team.name}
              </Typography>
              <Typography variant="body1">
                📅 Games Played: {team.name}
              </Typography>
              <Typography variant="body1">✅ Wins: {team.name}</Typography>
              <Typography variant="body1">❌ Losses: {team.name}</Typography>
              <Typography variant="body1">
                🏀 Points Scored: {team.name}
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        {/* Tabla de jugadores */}
        <Grid item xs={12} md={8}>
          <Box mt={3}>
            <Typography
              variant="h5"
              gutterBottom
              sx={{ fontWeight: 'bold', color: theme.palette.text.primary }}
            >
              Players
            </Typography>
            {team.players?.length ? (
              <TableContainer
                component={Paper}
                sx={{
                  borderRadius: theme.shape.borderRadius,
                  boxShadow: theme.shadows[3],
                }}
              >
                <Table>
                  <TableHead>
                    <TableRow
                      sx={{ backgroundColor: theme.palette.primary.light }}
                    >
                      <TableCell
                        sx={{
                          fontWeight: 'bold',
                          color: theme.palette.primary.contrastText,
                        }}
                      >
                        Name
                      </TableCell>
                      <TableCell
                        sx={{
                          fontWeight: 'bold',
                          color: theme.palette.primary.contrastText,
                        }}
                      >
                        Position
                      </TableCell>
                      <TableCell
                        sx={{
                          fontWeight: 'bold',
                          color: theme.palette.primary.contrastText,
                        }}
                      >
                        Number
                      </TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {team.players.map((player, index) => (
                      <TableRow
                        key={player.id}
                        sx={{
                          backgroundColor:
                            index % 2 === 0 ? theme.palette.grey[100] : 'white',
                        }}
                      >
                        <TableCell>{player.firstName}</TableCell>
                        <TableCell>{player.secondName}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            ) : (
              <Typography variant="body1">No players registered.</Typography>
            )}
          </Box>
        </Grid>
      </Grid>
    </Box>
  );
};

export default TeamsDetails;
