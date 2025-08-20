import {
  Card,
  CardContent,
  Stack,
  Typography,
  Tooltip,
  IconButton,
} from '@mui/material';
import React, { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { AddIcon } from '../core/MUI/icons/icons';
import { RoutesNavigationViews } from '../core/routes-const';
import { useTeam } from '@/modules/team/hook/team.hook';
import { ITeamContextProps } from '@/modules/team/type/team';
import { TeamDashboard } from './dashboard';
import { NoTeamMessage } from './NoTeamMessage';

export const InfoTeam: React.FC = () => {
  const navigate = useNavigate();
  const { teams, getTeamsByFiltered }: ITeamContextProps = useTeam();
  useEffect(() => {
    if (!teams || teams.length === 0) {
      (async () => {
        await getTeamsByFiltered({});
      })();
    }
  }, [teams, getTeamsByFiltered]);
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
            Total de equipos: {teams?.length ?? 0}
          </Typography>
          <Stack direction="row" spacing={1}>
            <Tooltip title="Agregar Equipo">
              <IconButton
                color="success"
                onClick={() => navigate(`/${RoutesNavigationViews.Team}/crear`)}
              >
                <AddIcon />
              </IconButton>
            </Tooltip>
          </Stack>
        </Stack>
        {teams && teams.length > 0 ? <TeamDashboard /> : <NoTeamMessage />}
      </CardContent>
    </Card>
  );
};
