import { usePlayerSanction } from '@/modules/playerSanction/hook/playerSanction.hook';
import {
  InfoPlayerSanctionsProps,
  IPlayerSanctionContextProps,
} from '@/modules/playerSanction/type/playerSanction';
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
import { AddIcon } from '../core/MUI/icons/icons';
import { RoutesNavigationViews } from '../core/routes-const';
import { PlayerSanctionDashboard } from './dashboard';
import { NoPlayerSanctionMessage } from './NoPlayerSanctionMessage';

export const InfoPlayerSanction: React.FC<InfoPlayerSanctionsProps> = ({
  id,
  useWithPlayer,
  name,
}) => {
  const navigate = useNavigate();
  const {
    playerSanctions,
    getPlayerSanctionByFilter,
  }: IPlayerSanctionContextProps = usePlayerSanction();
  useEffect(() => {
    (async () => {
      await getPlayerSanctionByFilter({
        ...(useWithPlayer ? { playerId: id } : { matchId: id }),
      });
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
              Total de sanciones: {playerSanctions?.length ?? 0}
            </Typography>

            <Stack direction="row" spacing={1}>
              <Tooltip title="Agregar Sancion">
                <IconButton
                  color="success"
                  onClick={() =>
                    navigate(`/${RoutesNavigationViews.PlayerSanction}/crear`)
                  }
                >
                  <AddIcon />
                </IconButton>
              </Tooltip>
            </Stack>
          </Stack>

          {playerSanctions && playerSanctions.length > 0 ? (
            <PlayerSanctionDashboard />
          ) : (
            <NoPlayerSanctionMessage name={name} />
          )}
        </CardContent>
      </Card>
    </>
  );
};
