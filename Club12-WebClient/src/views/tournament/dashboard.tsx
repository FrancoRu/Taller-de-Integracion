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
import { RenderPopupToDeleteTournament } from './CRUD/delete-tournament';
import { RoutesNavigationViews } from '../core/routes-const';
import { EditIcon, DeleteIcon, AddIcon } from '../core/MUI/icons/icons';
import { IDivisionContextProps } from '@/modules/division/type/division';
import { useDivision } from '@/modules/division/hook/division.hook';
import { DivisionDashboard } from '../division/dashboard';

export const TournamentDashboard: React.FC = () => {
  const { id } = useParams<{ id: GUID }>();
  const { setMessage } = useError();
  const navigate = useNavigate();

  const { tournament, getTournamentById }: ITournamentContextProps =
    useTournament();
  const { divisions }: IDivisionContextProps = useDivision();

  useEffect(() => {
    if (!id) {
      setMessage(400, ['Id not found']);
      navigate('/', { replace: true });
    }
  }, [id, navigate, setMessage]);

  if (!id) return;

  useEffect(() => {
    (async () => {
      const tournamentFound = await getTournamentById(id);

      if (!tournamentFound) {
        setMessage(400, ['Tournament not found']);
        navigate('/', { replace: true });
      }
    })();
  }, [id]);

  if (!tournament) {
    return <LoadingIndicator />;
  }

  return (
    <>
      <TournamentTitle {...tournament} />
      <Card
        sx={{
          width: '98%',
          mx: 'auto',
          px: { xs: 2, sm: 3, md: 4 },
        }}
      >
        <CardContent>
          <Typography variant="h6">
            Total de divisiones en el torneo: {divisions?.length}
          </Typography>
          <DivisionDashboard />
        </CardContent>
      </Card>
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
          <Typography variant="h6" fontWeight="bold">
            Torneo: {name}
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

            <Tooltip title="Agregar División">
              <IconButton
                color="success"
                onClick={() =>
                  navigate(`${RoutesNavigationViews.Division}/crear`)
                }
              >
                <AddIcon />
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
        <RenderPopupToDeleteTournament
          id={id}
          fn={deleteTournamentById}
          onClose={() => setShowPopup(false)}
        />
      )}
    </Card>
  );
};
