import { IStageResponse } from '@/modules/stage/type/stage';
import React, { useEffect, useState } from 'react';
import { RoutesNavigationViews } from '../core/routes-const';
import {
  Card,
  CardContent,
  Stack,
  Typography,
  Tooltip,
  IconButton,
} from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { AddIcon, SettingsSuggestIcon } from '../core/MUI/icons/icons';
import { NoMatchesMessage } from './message/NoMatchesMessage';
import { GenerateMatch } from './CRUD/generate-match';
import { MatchDashboard } from './dashboard';
import { IMatchContextProps } from '@/modules/match/type/match';
import { useMatch } from '@/modules/match/hook/match.hook';

export const InfoMatch: React.FC<IStageResponse> = ({ id, name }) => {
  const navigate = useNavigate();
  const [showPopup, setShowPopup] = useState<boolean>(false);
  const { matches, getMatchByFilter }: IMatchContextProps = useMatch();

  useEffect(() => {
    (async () => {
      await getMatchByFilter({ stageId: id });
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
              Total de partidos: {matches?.length ?? 0}
            </Typography>

            <Stack direction="row" spacing={1}>
              <Tooltip title="Agregar Partido">
                <IconButton
                  color="success"
                  onClick={() =>
                    navigate(`/${RoutesNavigationViews.Match}/crear`)
                  }
                >
                  <AddIcon />
                </IconButton>
              </Tooltip>
              <Tooltip title="Generar partidos automáticamente">
                <IconButton color="success" onClick={() => setShowPopup(true)}>
                  <SettingsSuggestIcon />
                </IconButton>
              </Tooltip>
            </Stack>
          </Stack>

          {matches && matches.length > 0 ? (
            <MatchDashboard matches={matches} />
          ) : (
            <NoMatchesMessage name={name} />
          )}
        </CardContent>
      </Card>
      {showPopup && (
        <GenerateMatch
          id={id}
          onClose={() => setShowPopup(false)}
        ></GenerateMatch>
      )}
    </>
  );
};
