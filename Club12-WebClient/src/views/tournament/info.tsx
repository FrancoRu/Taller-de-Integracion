import {
  Card,
  CardContent,
  Stack,
  Typography,
  Tooltip,
  IconButton,
  Grid,
} from '@mui/material';
import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  AddIcon,
  ArrowForwardIcon,
  DeleteIcon,
  EditIcon,
} from '../core/MUI/icons/icons';
import { RoutesNavigationViews } from '../core/routes-const';
import {
  ITournamentContextProps,
  ITournamentResponse,
} from '@/modules/tournament/type/tournament';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { NoTournamentMessage } from './NoTournamentMessage';
import { DeleteTournament } from './CRUD/delete-tournament';

export const InfoTournament: React.FC = () => {
  const navigate = useNavigate();
  const { tournaments, getAllTournamentsByFilter }: ITournamentContextProps =
    useTournament();
  useEffect(() => {
    if (!tournaments || tournaments.length === 0) {
      (async () => {
        await getAllTournamentsByFilter({});
      })();
    }
  }, []);

  return (
    <Card>
      <CardContent>
        <Stack
          direction="row"
          justifyContent="space-between"
          alignItems="center"
          mb={1}
        >
          <Typography variant="h6">
            Total de torneos: {tournaments?.length}
          </Typography>
          <Stack direction="row" spacing={1}>
            <Tooltip title="Agregar Torneo">
              <IconButton
                color="success"
                onClick={() =>
                  navigate(`/${RoutesNavigationViews.Tournament}/crear`)
                }
              >
                <AddIcon />
              </IconButton>
            </Tooltip>
          </Stack>
        </Stack>

        {tournaments && tournaments.length > 0 ? (
          <Grid container spacing={3} sx={{ px: 2, py: 3 }}>
            {tournaments.map(t => (
              <Grid item key={t.id} xs={12} sm={8} md={4}>
                <RenderTournament {...t} />
              </Grid>
            ))}
          </Grid>
        ) : (
          <NoTournamentMessage />
        )}
      </CardContent>
    </Card>
  );
};

const RenderTournament: React.FC<ITournamentResponse> = ({
  id,
  name,
  description,
  divisions,
}) => {
  const { deleteTournamentById } = useTournament();
  const navigate = useNavigate();
  const [showPopup, setShowPopup] = useState(false);
  const [isAnimating, setIsAnimating] = useState(false);

  const handleNavigate = () => {
    setIsAnimating(true);
    setTimeout(() => {
      navigate(`/${RoutesNavigationViews.Tournament}/${id}`);
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
            {name}
          </Typography>

          <Typography variant="body2" color="text.secondary" align="center">
            {description || 'Sin descripción'}
          </Typography>

          <Typography variant="caption" color="text.secondary" align="center">
            Divisiones: {divisions?.length ?? 0}
          </Typography>

          <Stack
            direction="row"
            spacing={1}
            justifyContent="center"
            sx={{ mt: 1 }}
          >
            <Tooltip title="Ver Torneo">
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
                  <ArrowForwardIcon titleAccess="Ver Torneo" />
                </IconButton>
              </span>
            </Tooltip>

            <Tooltip title="Editar Torneo">
              <IconButton
                color="secondary"
                onClick={() =>
                  navigate(`/${RoutesNavigationViews.Tournament}/${id}/editar`)
                }
              >
                <EditIcon titleAccess="Editar Torneo" />
              </IconButton>
            </Tooltip>

            <Tooltip title="Eliminar Torneo">
              <IconButton color="error" onClick={() => setShowPopup(true)}>
                <DeleteIcon titleAccess="Eliminar Torneo" />
              </IconButton>
            </Tooltip>
          </Stack>

          {showPopup && (
            <DeleteTournament
              id={id}
              fn={deleteTournamentById}
              onClose={() => setShowPopup(false)}
            />
          )}
        </Stack>
      </CardContent>
    </Card>
  );
};
