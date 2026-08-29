import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Box, Button, Stack, Typography } from '@mui/material';
import { IVenueResponse } from '@/modules/venue/type/venue';
import { useVenue } from '@/modules/venue/hook/venue.hook';
import PageShell from '@/views/core/components/PageShell';
import { DetailSkeleton } from '@/views/core/components/skeletons';
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
      <PageShell title="Cancha">
        <DetailSkeleton />
      </PageShell>
    );
  }

  if (!venue) {
    return (
      <PageShell
        title="Cancha no encontrada"
        back={{ label: 'Volver', onClick: () => navigate(APP_ROUTES.panelVenues) }}
      >
        <Typography sx={{ color: 'text.secondary' }}>
          No se pudo obtener la información de la cancha solicitada.
        </Typography>
      </PageShell>
    );
  }

  const hasCoordinates =
    venue.latitude !== undefined &&
    venue.latitude !== null &&
    venue.longitude !== undefined &&
    venue.longitude !== null;

  return (
    <PageShell
      title={venue.name}
      back={{ label: 'Volver', onClick: () => navigate(APP_ROUTES.panelVenues) }}
    >
      <Stack spacing={2}>
        <Stack spacing={1}>
          <Typography variant="subtitle2" sx={{ color: 'text.secondary' }}>
            Dirección
          </Typography>
          <Typography>{venue.address || '—'}</Typography>
        </Stack>

        {hasCoordinates && (
          <Button
            variant="outlined"
            component="a"
            href={`https://www.google.com/maps?q=${venue.latitude},${venue.longitude}`}
            target="_blank"
            rel="noopener noreferrer"
            sx={{ alignSelf: 'flex-start' }}
          >
            Ver en el mapa
          </Button>
        )}

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
    </PageShell>
  );
};

export default VenuePage;
