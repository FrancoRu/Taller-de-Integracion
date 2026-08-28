import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Box,
  Button,
  Card,
  CardContent,
  CircularProgress,
  Divider,
  Stack,
  Typography,
} from '@mui/material';
import { IVenueResponse } from '@/modules/venue/type/venue';
import { useVenue } from '@/modules/venue/hook/venue.hook';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';

const VenuePage: React.FC = () => {
  const { venueId } = useParams<{ venueId: string }>();
  const navigate = useNavigate();
  const { getVenueById } = useVenue();

  const [loading, setLoading] = useState(true);
  const [venue, setVenue] = useState<IVenueResponse | null>(null);

  useEffect(() => {
    const fetch = async () => {
      if (!venueId) {
        setLoading(false);
        return;
      }

      setLoading(true);
      const response = await getVenueById(venueId);
      setVenue(response ?? null);
      setLoading(false);
    };

    void fetch();
  }, [venueId, getVenueById]);

  if (loading) {
    return (
      <Card>
        <CardContent>
          <Stack
            spacing={2}
            sx={{
              alignItems: "center",
              py: 4
            }}>
            <CircularProgress />
            <Typography>Cargando cancha...</Typography>
          </Stack>
        </CardContent>
      </Card>
    );
  }

  if (!venue) {
    return (
      <Card>
        <CardContent>
          <Stack spacing={2}>
            <Typography variant="h6">Cancha no encontrada</Typography>
            <Typography sx={{
              color: "text.secondary"
            }}>
              No se pudo obtener la información de la cancha solicitada.
            </Typography>
            <Box>
              <Button
                variant="contained"
                color="primary"
                onClick={() => navigate(APP_ROUTES.panelVenues)}
              >
                Volver
              </Button>
            </Box>
          </Stack>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardContent>
        <Stack spacing={2}>
          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            spacing={2}
            sx={{
              alignItems: { xs: 'flex-start', sm: 'center' },
              justifyContent: "space-between"
            }}>
            <Typography variant="h5">{venue.name}</Typography>
            <Button
              variant="contained"
              color="primary"
              onClick={() => navigate(APP_ROUTES.panelVenues)}
            >
              Volver
            </Button>
          </Stack>

          <Divider />

          <Stack spacing={1}>
            <Typography variant="subtitle2" sx={{
              color: "text.secondary"
            }}>
              Dirección
            </Typography>
            <Typography>{venue.address || '—'}</Typography>
          </Stack>

          {venue.photoUrl && (
            <Box
              component="img"
              src={venue.photoUrl}
              alt={`Cancha ${venue.name}`}
              sx={{
                width: '100%',
                maxWidth: 520,
                borderRadius: 2,
                objectFit: 'cover',
              }}
            />
          )}
        </Stack>
      </CardContent>
    </Card>
  );
};

export default VenuePage;
