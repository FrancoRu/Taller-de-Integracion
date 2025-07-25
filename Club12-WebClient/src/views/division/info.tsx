import { ITournamentResponse } from '@/modules/tournament/type/tournament';
import {
  Card,
  CardContent,
  IconButton,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material';
import { DivisionDashboard } from './dashboard';
import { AddIcon } from '../core/MUI/icons/icons';
import { RoutesNavigationViews } from '../core/routes-const';
import { useNavigate } from 'react-router-dom';

export const InfoDivision: React.FC<ITournamentResponse> = ({ divisions }) => {
  const navigate = useNavigate();
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
            Total de divisiones: {divisions?.length}
          </Typography>
          <Stack direction="row" spacing={1}>
            <Tooltip title="Agregar División">
              <IconButton
                color="success"
                onClick={() =>
                  navigate(`/${RoutesNavigationViews.Division}/crear`)
                }
              >
                <AddIcon />
              </IconButton>
            </Tooltip>
          </Stack>
        </Stack>
        <DivisionDashboard />
      </CardContent>
    </Card>
  );
};
