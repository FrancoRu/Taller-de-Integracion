import { usePlayer } from '@/modules/player/hook/player.hook';
import { ITeamResponse } from '@/modules/team/type/team';
import {
  Card,
  CardContent,
  IconButton,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material';
import React, { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { RoutesNavigationViews } from '../core/routes-const';
import { AddIcon } from '../core/MUI/icons/icons';
import { PlayerDashboard } from './dashboard';
import { NoPlayerMessage } from './NoPlayerMessage';

export const InfoPlayer: React.FC<ITeamResponse> = ({ name, id }) => {
  const navigate = useNavigate();
  const { players, getPlayersByFilter } = usePlayer();

  useEffect(() => {
    (async () => {
      await getPlayersByFilter({ teamId: id });
    })();
  }, [id]);
  return (
    <>
      <Card
        sx={{
          width: '98%',
          mx: 'auto',
          px: { xs: 2, sm: 3, md: 4 },
        }}
      >
        <CardContent>
          <Stack
            direction="row"
            justifyContent="space-between"
            alignItems="center"
            mb={1}
          >
            <Typography variant="h6">
              Total de jugadores: {players?.length ?? 0}
            </Typography>

            <Stack direction="row" spacing={1}>
              <Tooltip title="Agregar jugador">
                <IconButton
                  color="success"
                  onClick={() =>
                    navigate(`/${RoutesNavigationViews.Player}/crear`)
                  }
                >
                  <AddIcon />
                </IconButton>
              </Tooltip>
            </Stack>
          </Stack>

          {players && players.length > 0 ? (
            <PlayerDashboard />
          ) : (
            <NoPlayerMessage name={name} />
          )}
        </CardContent>
      </Card>
    </>
  );
};
