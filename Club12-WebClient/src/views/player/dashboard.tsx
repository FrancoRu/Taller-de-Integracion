import { usePlayer } from '@/modules/player/hook/player.hook';
import { IPlayerResponse } from '@/modules/player/type/player';
import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { RoutesNavigationViews } from '../core/routes-const';
import LoadingIndicator from '../core/components/LoadingIndicator';
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
import { NoPlayerMessage } from './NoPlayerMessage';
import { useTeam } from '@/modules/team/hook/team.hook';
import {
  ArrowForwardIcon,
  EditIcon,
  DeleteIcon,
} from '../core/MUI/icons/icons';
import { DeletePlayer } from './CRUD/delete-player';

export const PlayerDashboard: React.FC = () => {
  const { players, getPlayersByFilter } = usePlayer();

  const { team } = useTeam();
  const navigate = useNavigate();

  useEffect(() => {
    if (!team) {
      navigate(`/${RoutesNavigationViews.Home}`);
    }
    (async () => {
      await getPlayersByFilter({ teamId: team?.id });
    })();
  }, [team, navigate]);

  console.log(team);
  if (!team) return null;

  return (
    <Box>
      {players ? (
        players.length > 0 ? (
          <Grid container spacing={3} sx={{ px: 2, py: 3 }}>
            {players.map(p => (
              <Grid item key={p.id} xs={12} sm={8} md={4}>
                <RenderPlayer {...p} />
              </Grid>
            ))}
          </Grid>
        ) : (
          <NoPlayerMessage name={team.name} />
        )
      ) : (
        <LoadingIndicator />
      )}
    </Box>
  );
};

const RenderPlayer: React.FC<IPlayerResponse> = ({
  id,
  firstName,
  secondName,
  lastName,
  documentNumber,
  birthDate,
  phoneNumber,
  socialSecurity,
}) => {
  const { deletePlayerById } = usePlayer();
  const navigate = useNavigate();
  const [showPopup, setShowPopup] = useState(false);
  const [isAnimating, setIsAnimating] = useState(false);

  const handleNavigate = () => {
    setIsAnimating(true);
    setTimeout(() => {
      navigate(`/${RoutesNavigationViews.Player}/${id}`);
      setIsAnimating(false);
    }, 300);
  };

  const fullName = secondName?.trim()
    ? `${firstName} ${secondName} ${lastName}`
    : `${firstName} ${lastName}`;

  return (
    <Card
      sx={{
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
            {fullName}
          </Typography>

          <Typography variant="body2" color="text.secondary" align="center">
            Documento: {documentNumber}
          </Typography>

          {birthDate && (
            <Typography variant="body2" color="text.secondary" align="center">
              Fecha de Nacimiento: {new Date(birthDate).toLocaleDateString()}
            </Typography>
          )}

          {phoneNumber && (
            <Typography variant="body2" color="text.secondary" align="center">
              Teléfono: {phoneNumber}
            </Typography>
          )}

          {socialSecurity && (
            <Typography variant="body2" color="text.secondary" align="center">
              Seguridad Social: {socialSecurity}
            </Typography>
          )}

          <Stack direction="row" spacing={1} justifyContent="center">
            <Tooltip title="Ver Jugador">
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
                  <ArrowForwardIcon titleAccess="Ver Jugador" />
                </IconButton>
              </span>
            </Tooltip>

            <Tooltip title="Editar Jugador">
              <IconButton
                color="secondary"
                onClick={() =>
                  navigate(`/${RoutesNavigationViews.Player}/${id}/editar`)
                }
              >
                <EditIcon titleAccess="Editar Jugador" />
              </IconButton>
            </Tooltip>

            <Tooltip title="Eliminar Jugador">
              <IconButton color="error" onClick={() => setShowPopup(true)}>
                <DeleteIcon titleAccess="Eliminar Jugador" />
              </IconButton>
            </Tooltip>
          </Stack>

          {showPopup && (
            <DeletePlayer
              id={id}
              fn={deletePlayerById}
              onClose={() => setShowPopup(false)}
            />
          )}
        </Stack>
      </CardContent>
    </Card>
  );
};
