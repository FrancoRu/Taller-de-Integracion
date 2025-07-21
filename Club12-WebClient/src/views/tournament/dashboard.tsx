import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { ITournamentContextProps } from '@/modules/tournament/type/tournament';
import React, { useEffect } from 'react';
import { Outlet, useNavigate, useParams } from 'react-router-dom';
import { GUID } from '@/modules/core/types/types.d';
import { useError } from '@/modules/error/hooks/error.hock';
import { CustomBox } from '../core/customsThemes/CustomBox';
import LoadingIndicator from '../core/components/LoadingIndicator';
import { Button, Typography } from '@mui/material';
import { DivisionDashboard } from '../division/dashboard';

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

  useEffect(() => {
    if (!id) return;

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
    <CustomBox>
      <TournamentTitle {...tournament} />
      <Button variant="contained" onClick={() => navigate('editar')}>
        Editar
      </Button>

      <DivisionDashboard />
      <Outlet />
    </CustomBox>
  );
};

const TournamentTitle = ({ name }: { name: string }) => (
  <Typography fontWeight={'bold'}>{name}</Typography>
);
