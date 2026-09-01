import { useCallback, useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Box, Button, Grid, Paper, Stack, Typography } from '@mui/material';
import { IVenueResponse, IPutVenueRequest } from '@/modules/venue/type/venue';
import { useVenue } from '@/modules/venue/hook/venue.hook';
import PageShell from '@/views/core/components/PageShell';
import LeafletMap from '@/views/core/components/LeafletMap';
import { DetailSkeleton } from '@/views/core/components/skeletons';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { notifySuccess, notifyWarning } from '@/modules/core/utils/confirmDialog';
import VenueFormDialog from '@/views/venue/VenueFormDialog';
import type { VenueFormField, VenueFormState } from '@/views/venue/venues.types';

const parseCoordinate = (value: string): number | undefined => {
  const trimmed = value.trim();
  if (trimmed === '') {
    return undefined;
  }
  const parsed = Number(trimmed);
  return Number.isFinite(parsed) ? parsed : undefined;
};

const EMPTY_VENUE_FORM: VenueFormState = {
  name: '',
  address: '',
  latitude: '',
  longitude: '',
  photo: null,
  photoUrl: '',
};

const VenuePage: React.FC = () => {
  const { venueId } = useParams<{ venueId: string }>();
  const navigate = useNavigate();
  const { getVenueById, putVenueById, putVenuePhotoById } = useVenue();

  const [loading, setLoading] = useState(true);
  const [venue, setVenue] = useState<IVenueResponse | null>(null);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [editSubmitting, setEditSubmitting] = useState(false);
  const [venueForm, setVenueForm] = useState<VenueFormState>(EMPTY_VENUE_FORM);

  const fetchVenue = useCallback(async () => {
    if (!venueId) {
      setLoading(false);
      return;
    }

    setLoading(true);
    const response = await getVenueById(venueId);
    setVenue(response ?? null);
    setLoading(false);
  }, [venueId, getVenueById]);

  useEffect(() => {
    void fetchVenue();
  }, [fetchVenue]);

  const handleVenueFieldChange = useCallback((field: VenueFormField, value: string) => {
    setVenueForm(prev => ({ ...prev, [field]: value }));
  }, []);

  const handlePhotoChange = useCallback((file: File | null) => {
    setVenueForm(prev => ({ ...prev, photo: file }));
  }, []);

  const openEditDialog = () => {
    if (!venue) return;

    setVenueForm({
      name: venue.name,
      address: venue.address,
      latitude: venue.latitude?.toString() ?? '',
      longitude: venue.longitude?.toString() ?? '',
      photo: null,
      photoUrl: venue.photoUrl ?? '',
    });
    setEditDialogOpen(true);
  };

  const handleEditSubmit = async () => {
    if (!venue) return;

    if (!venueForm.name.trim() || !venueForm.address.trim()) {
      void notifyWarning({
        title: 'Campos incompletos',
        text: 'Nombre y dirección son obligatorios.',
      });
      return;
    }

    setEditSubmitting(true);
    const payload: IPutVenueRequest = {
      name: venueForm.name.trim(),
      address: venueForm.address.trim(),
      latitude: parseCoordinate(venueForm.latitude),
      longitude: parseCoordinate(venueForm.longitude),
    };

    const updated = await putVenueById(venue.id, payload);

    if (!updated) {
      setEditSubmitting(false);
      return;
    }

    // The venue fields and its photo are two separate endpoints; upload the
    // new photo (if the admin picked one) as part of the same save.
    if (venueForm.photo) {
      await putVenuePhotoById(venue.id, venueForm.photo);
    }
    setEditSubmitting(false);

    setEditDialogOpen(false);
    await fetchVenue();
    await notifySuccess({
      title: 'Cancha actualizada',
      text: 'La cancha se actualizó correctamente.',
    });
  };

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

  // Beyond finiteness, an out-of-range value (e.g. a corrupted geocode that
  // lost its decimal point, like -6052382618888541 instead of -60.52...)
  // sends Leaflet a technically-numeric but physically impossible longitude.
  // Its Web Mercator projection math doesn't throw on that — it hangs the
  // tab computing/requesting tiles across an astronomically wide range.
  const hasCoordinates =
    Number.isFinite(venue.latitude) &&
    Number.isFinite(venue.longitude) &&
    Math.abs(venue.latitude as number) <= 90 &&
    Math.abs(venue.longitude as number) <= 180;

  return (
    <PageShell
      title={venue.name}
      back={{ label: 'Volver', onClick: () => navigate(APP_ROUTES.panelVenues) }}
      actions={
        <Button variant="outlined" color="primary" onClick={openEditDialog}>
          Editar cancha
        </Button>
      }
    >
      <Stack spacing={3}>
        {venue.photoUrl ? (
          <Box
            component="img"
            src={venue.photoUrl}
            alt={`Cancha ${venue.name}`}
            sx={{
              width: '100%',
              height: { xs: 200, sm: 280 },
              borderRadius: 2,
              objectFit: 'cover',
            }}
          />
        ) : (
          <Box
            sx={{
              width: '100%',
              height: { xs: 160, sm: 200 },
              borderRadius: 2,
              bgcolor: 'action.hover',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            <Typography variant="body2" sx={{ color: 'text.secondary' }}>
              Esta cancha todavía no tiene una foto cargada.
            </Typography>
          </Box>
        )}

        <Grid container spacing={2}>
          <Grid size={{ xs: 12, md: hasCoordinates ? 5 : 12 }}>
            <Paper variant="outlined" sx={{ p: 2, height: '100%' }}>
              <Typography variant="subtitle2" sx={{ color: 'text.secondary' }}>
                Dirección
              </Typography>
              <Typography sx={{ mb: hasCoordinates ? 0 : undefined }}>
                {venue.address || '—'}
              </Typography>
              {!hasCoordinates && (
                <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                  Esta cancha todavía no tiene coordenadas cargadas — editala para
                  ubicarla en el mapa.
                </Typography>
              )}
            </Paper>
          </Grid>

          {hasCoordinates && (
            <Grid size={{ xs: 12, md: 7 }}>
              <LeafletMap
                latitude={venue.latitude as number}
                longitude={venue.longitude as number}
                title={venue.name}
                height={280}
              />
            </Grid>
          )}
        </Grid>
      </Stack>

      <VenueFormDialog
        withPhoto
        open={editDialogOpen}
        title="Editar cancha"
        confirmLabel="Guardar"
        form={venueForm}
        submitting={editSubmitting}
        onFieldChange={handleVenueFieldChange}
        onPhotoChange={handlePhotoChange}
        onClose={() => setEditDialogOpen(false)}
        onConfirm={() => void handleEditSubmit()}
      />
    </PageShell>
  );
};

export default VenuePage;
