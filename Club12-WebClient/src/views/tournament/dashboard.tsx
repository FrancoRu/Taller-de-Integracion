import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import {
  ITournamentContextProps,
  ITournamentResponse,
} from '@/modules/tournament/type/tournament';
import React, { useEffect, useState } from 'react';
import { Outlet, useNavigate, useParams } from 'react-router-dom';
import { GUID } from '@/modules/core/types/types.d';
import { useError } from '@/modules/error/hooks/error.hock';
import LoadingIndicator from '../core/components/LoadingIndicator';
import {
  Card,
  CardContent,
  IconButton,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material';
import { DeleteTournament } from './CRUD/delete-tournament';
import { RoutesNavigationViews } from '../core/routes-const';
import { EditIcon, DeleteIcon } from '../core/MUI/icons/icons';
import { InfoDivision } from '../division/info';

export const TournamentDashboard: React.FC = () => {
  const { id } = useParams<{ id: GUID }>();
  const { setMessage } = useError();
  const navigate = useNavigate();

  const { tournament, getTournamentById }: ITournamentContextProps =
    useTournament();
  useEffect(() => {
    if (!id) {
      setMessage(400, ['Id not found']);
      navigate('/', { replace: true });
    }
  }, [id, navigate, setMessage]);

  if (!id) return;

  useEffect(() => {
    (async () => {
      if (!tournament || tournament.id !== id) {
        const tournamentFound = await getTournamentById(id);

        if (!tournamentFound) {
          setMessage(400, ['Tournament not found']);
          navigate('/', { replace: true });
        }
      }
    })();
  }, [id, tournament]);

  if (!tournament) {
    return <LoadingIndicator />;
  }

  return (
    <>
      <TournamentTitle {...tournament} />
      <InfoDivision {...tournament} />
      <Outlet />
    </>
  );
};
const TournamentTitle: React.FC<ITournamentResponse> = ({
  id,
  name,
  description,
}) => {
  const navigate = useNavigate();
  const { deleteTournamentById } = useTournament();
  const [showPopup, setShowPopup] = useState(false);

  return (
    <Card>
      <CardContent>
        <Stack
          direction="row"
          justifyContent="space-between"
          alignItems="center"
          mb={1}
        >
          <Typography variant="h6" fontWeight="bold">
            {name}
          </Typography>

          <Stack direction="row" spacing={1}>
            <Tooltip title="Editar Torneo">
              <IconButton
                color="primary"
                onClick={() =>
                  navigate(`/${RoutesNavigationViews.Tournament}/${id}/editar`)
                }
              >
                <EditIcon />
              </IconButton>
            </Tooltip>

            <Tooltip title="Eliminar Torneo">
              <IconButton color="error" onClick={() => setShowPopup(true)}>
                <DeleteIcon />
              </IconButton>
            </Tooltip>
          </Stack>
        </Stack>

        {description && (
          <Typography variant="body2" color="text.secondary">
            {description}
          </Typography>
        )}
      </CardContent>

      {showPopup && (
        <DeleteTournament
          id={id}
          fn={deleteTournamentById}
          onClose={() => setShowPopup(false)}
        />
      )}
    </Card>
  );
};
