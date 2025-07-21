import { IDivisionResponse } from '@/modules/division/type/division.d';
import React, { useEffect } from 'react';
import { CustomBox } from '../core/customsThemes/CustomBox';
import { Button, Card, CardContent, Stack, Typography } from '@mui/material';
import LoadingIndicator from '../core/components/LoadingIndicator';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { useNavigate, useParams } from 'react-router-dom';
import { RoutesNavigationViews } from '../core/routes-const';
import { useDivision } from '@/modules/division/hook/division.hook';
import { GUID } from '@/modules/core/types/types';

export const DivisionDashboard: React.FC = () => {
  const { divisions, getDivisionsByFilters } = useDivision();
  const { tournament } = useTournament();
  const { id } = useParams<{ id: GUID }>();
  const navigate = useNavigate();
  useEffect(() => {
    if (!tournament) {
      navigate('/');
    }
  }, [tournament, navigate]);

  if (!tournament) return null;
  useEffect(() => {
    (async () => {
      await getDivisionsByFilters({ tournamentId: id });
    })();
  }, [tournament]);

  return !divisions ? (
    <CustomBox>
      <Typography>Divisiones no encontradas para este torneo.</Typography>
    </CustomBox>
  ) : (
    <CustomBox>
      {divisions ? (
        divisions.length > 0 ? (
          divisions.map(d => <RenderDivision key={d.id} {...d} />)
        ) : (
          <RenderNoDivision name={tournament.name} />
        )
      ) : (
        <LoadingIndicator />
      )}
    </CustomBox>
  );
};

const RenderDivision: React.FC<IDivisionResponse> = ({
  id,
  name,
  isFinished,
  tournamentId,
}) => {
  const navigate = useNavigate();

  const handleNavigate = () => {
    navigate(
      `/${RoutesNavigationViews.Tournament}/${tournamentId}/${RoutesNavigationViews.Division}/${id}`
    );
  };
  return (
    <Card
      sx={{
        backgroundColor: isFinished ? 'grey.200' : 'background.paper',
        opacity: isFinished ? 0.6 : 1,
        border: isFinished ? '2px solid #ccc' : '2px solid',
        borderColor: isFinished ? 'grey.400' : 'primary.main',
        transition: 'transform 0.2s',
        '&:hover': {
          transform: 'scale(1.02)',
        },
      }}
    >
      <CardContent>
        <Stack spacing={2} alignItems="center">
          <Typography variant="h6" align="center" color="text.primary">
            {name}
          </Typography>

          <Button
            variant="contained"
            color="primary"
            disabled={isFinished}
            onClick={handleNavigate}
          >
            Seleccionar
          </Button>
          <Button
            variant="contained"
            onClick={() =>
              navigate(`${RoutesNavigationViews.Division}/${id}/editar`)
            }
          >
            Editar
          </Button>
        </Stack>
      </CardContent>
    </Card>
  );
};

const RenderNoDivision: React.FC<{ name: string }> = ({ name }) => {
  return (
    <Typography>No se encontraron division para el Torneo: {name}</Typography>
  );
};
