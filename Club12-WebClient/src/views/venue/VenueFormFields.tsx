import { useEffect, useRef, useState } from 'react';
import { Box, Button, FormLabel, Stack, TextField, Typography } from '@mui/material';
import { geocodeAddress } from '@/modules/core/utils/geocoding';
import { FILTERS_DEBOUNCE_DELAY_LONG_MS } from '@/modules/core/constants/constants';
import LeafletMap from '@/views/core/components/LeafletMap';
import type { VenueFormField, VenueFormState } from '@/views/venue/venues.types';

const COORDINATES_HELPER_TEXT =
  'Se completan solas al escribir la dirección; podés ajustar el pin en el mapa o las coordenadas a mano.';

/** Paraná, Entre Ríos — the league's home city, used as a sensible default
 * map center for a brand-new venue with no address/coordinates yet. */
const DEFAULT_LATITUDE = -31.7333;
const DEFAULT_LONGITUDE = -60.5297;

export interface VenueFormFieldsProps {
  withPhoto: boolean;
  form: VenueFormState;
  onFieldChange: (field: VenueFormField, value: string) => void;
  onPhotoChange: (file: File | null) => void;
}

/**
 * The name/address/coordinates/photo fields shared by every place a venue
 * gets created or edited — a single source of truth so a venue created from
 * the standalone Canchas admin form and one edited from its detail page stay
 * on the same required fields.
 */
export default function VenueFormFields({
  withPhoto,
  form,
  onFieldChange,
  onPhotoChange,
}: VenueFormFieldsProps) {
  // Preview a freshly picked photo immediately (object URL), falling back to
  // the venue's stored photo. The URL is revoked when the file changes or the
  // component unmounts so it doesn't leak.
  const [photoPreview, setPhotoPreview] = useState('');
  useEffect(() => {
    if (!form.photo) {
      setPhotoPreview('');
      return;
    }
    const url = URL.createObjectURL(form.photo);
    setPhotoPreview(url);
    return () => URL.revokeObjectURL(url);
  }, [form.photo]);
  const displayedPhotoUrl = photoPreview || form.photoUrl;

  const parsedLatitude = Number(form.latitude);
  const parsedLongitude = Number(form.longitude);
  const hasCoordinates =
    form.latitude.trim() !== '' &&
    form.longitude.trim() !== '' &&
    Number.isFinite(parsedLatitude) &&
    Number.isFinite(parsedLongitude);
  const mapLatitude = hasCoordinates ? parsedLatitude : DEFAULT_LATITUDE;
  const mapLongitude = hasCoordinates ? parsedLongitude : DEFAULT_LONGITUDE;

  // Auto-geocode the typed address (debounced) so the map recenters on its
  // own — no "buscar" button to click. Only fires again when the address
  // text itself changes, so a pin the admin just dragged/clicked into place
  // isn't clobbered by an unrelated re-render.
  const lastGeocodedAddress = useRef('');
  useEffect(() => {
    const trimmed = form.address.trim();
    if (!trimmed || trimmed === lastGeocodedAddress.current) {
      return;
    }

    const timeoutId = setTimeout(() => {
      void geocodeAddress(trimmed).then(result => {
        if (!result) return;
        lastGeocodedAddress.current = trimmed;
        onFieldChange('latitude', String(result.latitude));
        onFieldChange('longitude', String(result.longitude));
      });
    }, FILTERS_DEBOUNCE_DELAY_LONG_MS);

    return () => clearTimeout(timeoutId);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [form.address]);

  const handlePinMoved = (latitude: number, longitude: number) => {
    onFieldChange('latitude', String(latitude));
    onFieldChange('longitude', String(longitude));
  };

  return (
    <Stack spacing={2} sx={{ mt: 1 }}>
      {withPhoto && (
        <Box
          sx={{
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            gap: 1,
          }}
        >
          <FormLabel>Foto</FormLabel>
          {displayedPhotoUrl ? (
            <Box
              component="img"
              src={displayedPhotoUrl}
              alt="Vista previa de la cancha"
              sx={{
                width: '100%',
                maxWidth: 320,
                height: 160,
                borderRadius: 2,
                objectFit: 'cover',
              }}
            />
          ) : (
            <Box
              sx={{
                width: '100%',
                maxWidth: 320,
                height: 160,
                borderRadius: 2,
                bgcolor: 'action.hover',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                Sin foto
              </Typography>
            </Box>
          )}
          <Button variant="outlined" component="label" size="small">
            {displayedPhotoUrl ? 'Cambiar foto' : 'Seleccionar foto'}
            <input
              hidden
              type="file"
              accept="image/*"
              onChange={event => {
                const selectedFile = event.target.files?.[0] ?? null;
                onPhotoChange(selectedFile);
              }}
            />
          </Button>
          {form.photo && (
            <Typography variant="caption" sx={{ color: 'text.secondary' }}>
              {form.photo.name}
            </Typography>
          )}
        </Box>
      )}

      <TextField
        label="Nombre"
        value={form.name}
        onChange={e => onFieldChange('name', e.target.value)}
        required
        fullWidth
      />
      <TextField
        label="Dirección"
        value={form.address}
        onChange={e => onFieldChange('address', e.target.value)}
        required
        fullWidth
      />

      <Box>
        <FormLabel sx={{ display: 'block', mb: 1 }}>Ubicación en el mapa</FormLabel>
        <LeafletMap
          latitude={mapLatitude}
          longitude={mapLongitude}
          title={form.name || 'la cancha'}
          height={280}
          onLocationChange={handlePinMoved}
        />
        <Typography variant="caption" sx={{ color: 'text.secondary' }}>
          Hacé click en el mapa o arrastrá el pin para ajustar la ubicación exacta.
        </Typography>
      </Box>

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
        <TextField
          label="Latitud"
          type="number"
          value={form.latitude}
          onChange={e => onFieldChange('latitude', e.target.value)}
          fullWidth
          helperText={COORDINATES_HELPER_TEXT}
        />
        <TextField
          label="Longitud"
          type="number"
          value={form.longitude}
          onChange={e => onFieldChange('longitude', e.target.value)}
          fullWidth
        />
      </Stack>
    </Stack>
  );
}
