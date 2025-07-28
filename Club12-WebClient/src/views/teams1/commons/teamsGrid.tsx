import React, { useEffect, useState, useContext } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Grid,
  Card,
  CardContent,
  Typography,
  CardMedia,
  Box,
  useTheme,
  CircularProgress,
} from '@mui/material';
import { TeamContext } from '@/modules/team/context/team.context';
import { ITeamResponse } from '@/modules/team/type/team';
import { GUID } from '@/modules/core/types/types';

const TeamsGrid: React.FC = () => {
  const navigate = useNavigate();
  const theme = useTheme();

  const teamContext = useContext(TeamContext);

  if (!teamContext) {
    throw new Error('TeamsGrid must be used within a TeamProvider');
  }

  const { getTeamsByFiltered } = teamContext;

  const [teams, setTeams] = useState<ITeamResponse[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchTeams = async () => {
      setLoading(true);
      const result = await getTeamsByFiltered({ pageNumber: 1, pageSize: 100 });
      if (result?.items) {
        setTeams(result.items);
      }
      setLoading(false);
    };

    fetchTeams();
  }, [getTeamsByFiltered]);

  const handleTeamClick = (id: GUID) => {
    navigate(`/teams/${id}`);
  };

  if (loading) {
    return (
      <Box p={3} textAlign="center">
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box
      sx={{
        minHeight: '100vh',
        backgroundColor: theme.palette.background.default,
        padding: 3,
      }}
    >
      <Grid container spacing={3}>
        <Grid item xs={12} sm={6} md={4}>
          <Card
            onClick={() => navigate('/equipos/crear')}
            sx={{
              cursor: 'pointer',
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              borderRadius: theme.shape.borderRadius,
              justifyContent: 'center',
              height: 210,
              border: `2px dashed ${theme.palette.primary.light}`,
              backgroundColor: 'white',
              transition: '0.3s',
              '&:hover': {
                backgroundColor: theme.palette.primary.light,
                boxShadow: theme.shadows[5],
              },
            }}
          >
            <CardContent>
              <Typography
                variant="h4"
                align="center"
                sx={{ color: theme.palette.primary.main }}
              >
                +
              </Typography>
              <Typography
                variant="h6"
                align="center"
                sx={{ color: theme.palette.text.primary }}
              >
                Add New Team
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        {teams.map(team => (
          <Grid item xs={12} sm={6} md={4} key={team.id}>
            <Card
              onClick={() => handleTeamClick(team.id)}
              sx={{
                cursor: 'pointer',
                borderRadius: theme.shape.borderRadius,
                boxShadow: theme.shadows[3],
                transition: '0.3s',
                background: 'white',
                '&:hover': {
                  boxShadow: theme.shadows[6],
                  transform: 'scale(1.03)',
                },
              }}
            >
              <CardMedia
                component="img"
                height="140"
                image={team.logoUrl || '/placeholder-image.jpg'}
                alt={team.name}
                sx={{
                  borderTopLeftRadius: theme.shape.borderRadius,
                  borderTopRightRadius: theme.shape.borderRadius,
                }}
              />
              <CardContent>
                <Typography
                  variant="h6"
                  align="center"
                  sx={{ fontWeight: 'bold', color: theme.palette.text.primary }}
                >
                  {team.name}
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>
    </Box>
  );
};

export default TeamsGrid;
