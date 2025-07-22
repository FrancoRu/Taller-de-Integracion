import { IDivisionResponse } from '@/modules/division/type/division.d';
import React, { useEffect, useState } from 'react';
import { CustomBox } from '../core/MUI/customsThemes/CustomBox';
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
import LoadingIndicator from '../core/components/LoadingIndicator';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { useNavigate, useParams } from 'react-router-dom';
import { RoutesNavigationViews } from '../core/routes-const';
import { useDivision } from '@/modules/division/hook/division.hook';
import { GUID } from '@/modules/core/types/types';
import { RenderPopupToDeleteDivision } from './CRUD/delete-division';
import { EditIcon, DeleteIcon, ArrowForward } from '../core/MUI/icons/icons';
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
  }, [id]);

  return !divisions ? (
    <CustomBox>
      <Typography>Divisiones no encontradas para este torneo.</Typography>
    </CustomBox>
  ) : (
    <Box>
      {divisions ? (
        divisions.length > 0 ? (
          <Grid container spacing={3} sx={{ px: 2, py: 3 }}>
            {divisions.map(d => (
              <Grid item key={d.id} xs={12} sm={8} md={4}>
                <RenderDivision {...d} />
              </Grid>
            ))}
          </Grid>
        ) : (
          <RenderNoDivision name={tournament.name} />
        )
      ) : (
        <LoadingIndicator />
      )}
    </Box>
  );
};

const RenderDivision: React.FC<IDivisionResponse> = ({
  id,
  name,
  isFinished,
  tournamentId,
}) => {
  const { deleteDivisionsById } = useDivision();
  const navigate = useNavigate();
  const [showPopup, setShowPopup] = useState(false);
  const [isAnimating, setIsAnimating] = useState(false);

  const handleNavigate = () => {
    if (isFinished) return;

    setIsAnimating(true);
    setTimeout(() => {
      navigate(
        `/${RoutesNavigationViews.Tournament}/${tournamentId}/${RoutesNavigationViews.Division}/${id}`
      );
      setIsAnimating(false);
    }, 300);
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

          <Stack direction="row" spacing={1}>
            <Tooltip title="Seleccionar Division">
              <IconButton
                color="primary"
                disabled={isFinished || isAnimating}
                onClick={handleNavigate}
                sx={{
                  transition: 'transform 0.5s ease',
                  transform: isAnimating ? 'translateX(10px)' : 'translateX(0)',
                }}
              >
                <ArrowForward titleAccess="Seleccionar Division" />
              </IconButton>
            </Tooltip>

            <Tooltip title="Editar Division">
              <IconButton
                color="secondary"
                onClick={() =>
                  navigate(`${RoutesNavigationViews.Division}/${id}/editar`)
                }
              >
                <EditIcon titleAccess="Editar Division" />
              </IconButton>
            </Tooltip>

            <Tooltip title="Eliminar Division">
              <IconButton color="error" onClick={() => setShowPopup(true)}>
                <DeleteIcon titleAccess="Eliminar Division" />
              </IconButton>
            </Tooltip>
          </Stack>

          {showPopup && (
            <RenderPopupToDeleteDivision
              id={id}
              fn={deleteDivisionsById}
              onClose={() => setShowPopup(false)}
            />
          )}
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
