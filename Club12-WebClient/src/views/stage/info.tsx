import {
  Card,
  CardContent,
  Stack,
  Typography,
  Tooltip,
  IconButton,
} from '@mui/material';
import React, { useEffect, useState } from 'react';
import { AddIcon, SettingsSuggestIcon } from '../core/MUI/icons/icons';
import { RoutesNavigationViews } from '../core/routes-const';
import { StageDashboard } from './dashboard';
import { NoStagesMessage } from './NoStageMessage';
import { useNavigate } from 'react-router-dom';
import { GenerateStage } from './CRUD/generate-stage';
import { IDivisionResponse } from '@/modules/division/type/division';
import { IStageContextProps } from '@/modules/stage/type/stage';
import { useStage } from '@/modules/stage/hook/stage.hook';

export const InfoStage: React.FC<IDivisionResponse> = ({ id, name }) => {
  const navigate = useNavigate();
  const [showPopup, setShowPopup] = useState<boolean>(false);
  const { stages, getStagesByFilters }: IStageContextProps = useStage();
  useEffect(() => {
    (async () => {
      await getStagesByFilters({ divisionId: id });
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
              Total de fases: {stages?.length ?? 0}
            </Typography>

            <Stack direction="row" spacing={1}>
              <Tooltip title="Agregar Fase">
                <IconButton
                  color="success"
                  onClick={() =>
                    navigate(`/${RoutesNavigationViews.Stage}/crear`)
                  }
                >
                  <AddIcon />
                </IconButton>
              </Tooltip>
              <Tooltip title="Generar Fases Automáticamente">
                <IconButton color="success" onClick={() => setShowPopup(true)}>
                  <SettingsSuggestIcon />
                </IconButton>
              </Tooltip>
            </Stack>
          </Stack>

          {stages && stages.length > 0 ? (
            <StageDashboard stages={stages} />
          ) : (
            <NoStagesMessage name={name} />
          )}
        </CardContent>
      </Card>
      {showPopup && (
        <GenerateStage
          id={id}
          onClose={() => setShowPopup(false)}
        ></GenerateStage>
      )}
    </>
  );
};
