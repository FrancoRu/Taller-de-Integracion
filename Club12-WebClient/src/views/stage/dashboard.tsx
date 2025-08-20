import { IDashboardStage, IStageResponse } from '@/modules/stage/type/stage.d';
import {
  Card,
  CardContent,
  Stack,
  Typography,
  Tooltip,
  IconButton,
  Box,
  Grid,
  Chip,
} from '@mui/material';
import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  ArrowForwardIcon,
  EditIcon,
  DeleteIcon,
} from '../core/MUI/icons/icons';
import { RoutesNavigationViews } from '../core/routes-const';
import { useStage } from '@/modules/stage/hook/stage.hook';
import { DeleteStage } from './CRUD/delete-stage';
import { useDivision } from '@/modules/division/hook/division.hook';
import LoadingIndicator from '../core/components/LoadingIndicator';
import { NoStagesMessage } from './NoStageMessage';
import { translateStageType } from '@/modules/core/utils/translateStageType';

export const StageDashboard: React.FC<IDashboardStage> = ({ stages }) => {
  const { division } = useDivision();

  return (
    <Box>
      {stages ? (
        stages.length > 0 ? (
          <Grid container spacing={3} sx={{ px: 2, py: 3 }}>
            {stages.map(s => (
              <Grid item key={s.id} xs={12} sm={8} md={4}>
                <RenderStage {...s} />
              </Grid>
            ))}
          </Grid>
        ) : (
          <NoStagesMessage name={division!.name} />
        )
      ) : (
        <LoadingIndicator />
      )}
    </Box>
  );
};

const RenderStage: React.FC<IStageResponse> = ({
  id,
  name,
  startDate,
  endDate,
  description,
  isActive,
  isElimination,
  stageType,
}) => {
  const { deleteStagesById } = useStage();
  const navigate = useNavigate();
  const [showPopup, setShowPopup] = useState(false);
  const [isAnimating, setIsAnimating] = useState(false);

  const handleNavigate = () => {
    setIsAnimating(true);
    setTimeout(() => {
      navigate(`/${RoutesNavigationViews.Stage}/${id}`);
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

          <Typography variant="body2" color="text.secondary" align="center">
            {startDate} - {endDate}
          </Typography>

          <Typography
            variant="caption"
            color={isElimination ? 'error.main' : 'success.main'}
            align="center"
          >
            {isElimination ? 'Eliminatoria' : 'Fase Regular'} |{' '}
            {translateStageType(stageType)}
          </Typography>

          <Stack direction="row" spacing={1} justifyContent="center">
            <Tooltip title="Seleccionar Etapa">
              <span>
                <IconButton
                  color="primary"
                  onClick={handleNavigate}
                  sx={{
                    transition: 'transform 0.5s ease',
                    transform: isAnimating
                      ? 'translateX(10px)'
                      : 'translateX(0)',
                  }}
                >
                  <ArrowForwardIcon titleAccess="Seleccionar Etapa" />
                </IconButton>
              </span>
            </Tooltip>
            {isActive && (
              <>
                <Tooltip title="Editar Etapa">
                  <IconButton
                    color="secondary"
                    onClick={() =>
                      navigate(`/${RoutesNavigationViews.Stage}/${id}/editar`)
                    }
                  >
                    <EditIcon titleAccess="Editar Etapa" />
                  </IconButton>
                </Tooltip>
                <Tooltip title="Eliminar Etapa">
                  <IconButton color="error" onClick={() => setShowPopup(true)}>
                    <DeleteIcon titleAccess="Eliminar Etapa" />
                  </IconButton>
                </Tooltip>
              </>
            )}
          </Stack>

          {showPopup && (
            <DeleteStage
              id={id}
              fn={deleteStagesById}
              onClose={() => setShowPopup(false)}
            />
          )}
        </Stack>
        <Stack direction="row" spacing={1} justifyContent="center">
          {isActive ? (
            <Chip label="Activa" color="success" />
          ) : (
            <Chip label="Finalizada" color="error" />
          )}
        </Stack>
      </CardContent>
    </Card>
  );
};
