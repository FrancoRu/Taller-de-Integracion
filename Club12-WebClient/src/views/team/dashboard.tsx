import { useTeam } from '@/modules/team/hook/team.hook';
import { ITeamResponse } from '@/modules/team/type/team';
import LoadingIndicator from '../core/components/LoadingIndicator';
import { RoutesNavigationViews } from '../core/routes-const';
import {
  Box,
  Card,
  CardContent,
  Grid,
  IconButton,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material';
import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import ArrowForwardIcon from '@mui/icons-material/ArrowForward';
import { EditIcon, DeleteIcon } from '@/views/core/MUI/icons/icons';
import { DeleteTeam } from './CRUD/delete-team';

export const TeamDashboard: React.FC = () => {
  const { teams, getTeamsByFiltered } = useTeam();

  useEffect(() => {
    (async () => {
      await getTeamsByFiltered({});
    })();
  }, []);

  return (
    <Box>
      {teams ? (
        teams.length > 0 ? (
          <Grid container spacing={3} sx={{ px: 2, py: 3 }}>
            {teams.map(t => (
              <Grid item key={t.id} xs={12} sm={8} md={4}>
                <RenderTeam {...t} />
              </Grid>
            ))}
          </Grid>
        ) : (
          <Typography
            align="center"
            variant="h6"
            color="text.secondary"
            sx={{ mt: 3 }}
          >
            No hay equipos registrados.
          </Typography>
        )
      ) : (
        <LoadingIndicator />
      )}
    </Box>
  );
};

const RenderTeam: React.FC<ITeamResponse> = ({
  id,
  name,
  threeLetterCode,
  shirtColor,
  logoUrl,
}) => {
  const { deleteTeamById } = useTeam();
  const navigate = useNavigate();
  const [showPopup, setShowPopup] = useState(false);
  const [isAnimating, setIsAnimating] = useState(false);

  const handleNavigate = () => {
    setIsAnimating(true);
    setTimeout(() => {
      navigate(`/${RoutesNavigationViews.Team}/${id}`);
      setIsAnimating(false);
    }, 300);
  };

  return (
    <Card
      sx={{
        backgroundColor: 'background.paper',
        border: '2px solid',
        borderColor: 'primary.main',
        transition: 'transform 0.2s',
        '&:hover': {
          transform: 'scale(1.02)',
        },
      }}
    >
      <CardContent>
        <Stack spacing={1} alignItems="center">
          <Typography variant="h6" align="center" color="text.primary">
            {name} ({threeLetterCode})
          </Typography>

          <Typography variant="body2" color="text.secondary" align="center">
            Color de camiseta: {shirtColor || 'No especificado'}
          </Typography>

          {logoUrl && (
            <img
              src={logoUrl}
              alt={name}
              style={{
                width: '100%',
                maxHeight: '150px',
                objectFit: 'contain',
                borderRadius: 4,
              }}
            />
          )}

          <Stack
            direction="row"
            spacing={1}
            justifyContent="center"
            sx={{ mt: 1 }}
          >
            <Tooltip title="Ver Equipo">
              <span>
                <IconButton
                  color="primary"
                  disabled={isAnimating}
                  onClick={handleNavigate}
                  sx={{
                    transition: 'transform 0.5s ease',
                    transform: isAnimating
                      ? 'translateX(10px)'
                      : 'translateX(0)',
                  }}
                >
                  <ArrowForwardIcon titleAccess="Ver Equipo" />
                </IconButton>
              </span>
            </Tooltip>

            <Tooltip title="Editar Equipo">
              <IconButton
                color="secondary"
                onClick={() =>
                  navigate(`/${RoutesNavigationViews.Team}/${id}/editar`)
                }
              >
                <EditIcon titleAccess="Editar Equipo" />
              </IconButton>
            </Tooltip>

            <Tooltip title="Eliminar Equipo">
              <IconButton color="error" onClick={() => setShowPopup(true)}>
                <DeleteIcon titleAccess="Eliminar Equipo" />
              </IconButton>
            </Tooltip>
          </Stack>

          {showPopup && (
            <DeleteTeam
              id={id}
              fn={deleteTeamById}
              onClose={() => setShowPopup(false)}
            />
          )}
        </Stack>
      </CardContent>
    </Card>
  );
};
