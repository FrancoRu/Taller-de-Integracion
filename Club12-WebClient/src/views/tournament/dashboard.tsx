import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import {
  ITournamentContextProps,
  ITournamentResponse,
} from '@/modules/tournament/type/tournament';
import React, { useEffect, useState } from 'react';
import { Outlet, useNavigate, useParams } from 'react-router-dom';
import { GUID } from '@/modules/core/types/types';
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
import {
  EditIcon,
  DeleteIcon,
  AppRegistrationIcon,
} from '../core/MUI/icons/icons';
import { InfoDivision } from '../division/info';
import { isDeadlineInTheFuture } from '@/modules/core/utils/formatDate';

export const TournamentDashboard: React.FC = () => {
  const { tournamentId: id } = useParams<{ tournamentId: GUID }>();
  const { setMessage } = useError();
  const navigate = useNavigate();

  const { tournament, getTournamentById }: ITournamentContextProps =
    useTournament();

  if (!id) {
    navigate(`/${RoutesNavigationViews.Home}`, { replace: true });
    setMessage(400, [
      'Hubo un problema al cargar el torneo. Porfavor intentelo mas tarde',
    ]);
    return;
  }

  useEffect(() => {
    const fetchTournament = async () => {
      if (tournament && tournament.id === id) {
        return;
      }

      await getTournamentById(id);
    };

    fetchTournament();
  }, [id, tournament, getTournamentById, setMessage, navigate]);

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
  teamRegistrationDeadline,
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
            {isDeadlineInTheFuture(teamRegistrationDeadline) && (
              <Tooltip title="Registrar Equipos">
                <IconButton
                  color="primary"
                  onClick={() =>
                    navigate(
                      `/${RoutesNavigationViews.Tournament}/${id}/registro-equipos`
                    )
                  }
                >
                  <AppRegistrationIcon />
                </IconButton>
              </Tooltip>
            )}
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
