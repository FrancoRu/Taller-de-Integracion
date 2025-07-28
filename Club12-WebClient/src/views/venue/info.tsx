import {
  Card,
  CardContent,
  Stack,
  Typography,
  Tooltip,
  IconButton,
} from '@mui/material';
import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { AddIcon } from '../core/MUI/icons/icons';
import { RoutesNavigationViews } from '../core/routes-const';
import { useVenue } from '@/modules/venue/hook/venue.hook';
import { IVenueContextProps } from '@/modules/venue/type/venue';
import { VenueDashboard } from './dashboard';
import { NoVenueMessage } from './NoVenueMessage';

export const InfoVenue: React.FC = () => {
  const navigate = useNavigate();
  const { venues, getAllVenues }: IVenueContextProps = useVenue();
  useEffect(() => {
    if (!venues || venues.length === 0) {
      (async () => {
        await getAllVenues();
      })();
    }
  }, []);
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
            Total de canchas: {venues?.length}
          </Typography>
          <Stack direction="row" spacing={1}>
            <Tooltip title="Agregar Cancha">
              <IconButton
                color="success"
                onClick={() =>
                  navigate(`/${RoutesNavigationViews.Venue}/crear`)
                }
              >
                <AddIcon />
              </IconButton>
            </Tooltip>
          </Stack>
        </Stack>
        {venues && venues.length > 0 ? <VenueDashboard /> : <NoVenueMessage />}
      </CardContent>
    </Card>
  );
};
