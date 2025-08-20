import { useMatch } from '@/modules/match/hook/match.hook';
import { usePlayerSanction } from '@/modules/playerSanction/hook/playerSanction.hook';
import {
  IPlayerSanctionContextProps,
  IPlayerSanctionResponse,
} from '@/modules/playerSanction/type/playerSanction';
import React, { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { RoutesNavigationViews } from '../core/routes-const';
import {
  Card,
  CardContent,
  Grid,
  IconButton,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material';
import { NoPlayerSanctionMessage } from './NoPlayerSanctionMessage';
import { EditIcon, DeleteIcon } from '../core/MUI/icons/icons';
import { formatMatchDateToString } from '@/modules/core/utils/formatDate';
import { DeletePlayerSanction } from './CRUD/delete-playerSanction';

export const PlayerSanctionDashboard: React.FC = () => {
  const {
    playerSanctions,
    getPlayerSanctionByFilter,
  }: IPlayerSanctionContextProps = usePlayerSanction();

  const { match } = useMatch();
  const navigate = useNavigate();

  useEffect(() => {
    if (!match) {
      navigate(`/${RoutesNavigationViews.Home}`);
    }

    (async () => {
      await getPlayerSanctionByFilter({ matchId: match?.id });
    })();
  }, [match, navigate, playerSanctions]);
  return (
    <>
      {playerSanctions ? (
        <Grid container spacing={3} sx={{ px: 2, py: 3 }}>
          {playerSanctions.map(p => (
            <Grid item key={p.id} xs={12} sm={8} md={4}>
              <RenderPlayerSanction {...p} />
            </Grid>
          ))}
        </Grid>
      ) : (
        <NoPlayerSanctionMessage />
      )}
    </>
  );
};

const RenderPlayerSanction: React.FC<IPlayerSanctionResponse> = ({
  id,
  duration,
  issuedDate,
  description,
  playerId,
  playerFullName,
}) => {
  const { deletePlayerSanction } = usePlayerSanction();
  const navigate = useNavigate();
  const [showPopup, setShowPopup] = useState(false);

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
          <Typography variant="body2" color="text.secondary" align="center">
            Duración: {duration} Partidos
          </Typography>

          <Typography variant="body2" color="text.secondary" align="center">
            Fecha de emisión:{' '}
            {formatMatchDateToString(new Date(issuedDate).toISOString())}
          </Typography>

          <Typography variant="body2" color="text.secondary" align="center">
            Descripción: {description}
          </Typography>

          <Typography variant="body2" color="text.secondary" align="center">
            Jugador:{' '}
            <Link
              to={`/${RoutesNavigationViews.Player}/${playerId}`}
              style={{ textDecoration: 'none', color: 'inherit' }}
            >
              <strong>{playerFullName}</strong>
            </Link>
          </Typography>

          <Stack direction="row" spacing={1} justifyContent="center">
            <Tooltip title="Editar Sanción">
              <IconButton
                color="secondary"
                onClick={() =>
                  navigate(
                    `/${RoutesNavigationViews.PlayerSanction}/${id}/editar`
                  )
                }
              >
                <EditIcon titleAccess="Editar Sanción" />
              </IconButton>
            </Tooltip>

            <Tooltip title="Eliminar Sanción">
              <IconButton color="error" onClick={() => setShowPopup(true)}>
                <DeleteIcon titleAccess="Eliminar Sanción" />
              </IconButton>
            </Tooltip>
          </Stack>

          {showPopup && (
            <DeletePlayerSanction
              id={id}
              fn={deletePlayerSanction}
              onClose={() => setShowPopup(false)}
            />
          )}
        </Stack>
      </CardContent>
    </Card>
  );
};
