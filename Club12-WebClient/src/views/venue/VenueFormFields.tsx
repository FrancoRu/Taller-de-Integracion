import { useEffect, useState } from 'react';
import { Box, Button, FormLabel, Stack, TextField, Typography } from '@mui/material';
import { geocodeAddress } from '@/modules/core/utils/geocoding';
import { notifyWarning } from '@/modules/core/utils/confirmDialog';
import OpenStreetMapEmbed from '@/views/core/components/OpenStreetMapEmbed';
import type { VenueFormField, VenueFormState } from '@/views/venue/venues.types';

const COORDINATES_HELPER_TEXT =
  'Se completan automáticamente al buscar la dirección en el mapa; podés ajustarlas a mano si el resultado no es exacto.';

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

  const [geocoding, setGeocoding] = useState(false);
  const parsedLatitude = Number(form.latitude);
  const parsedLongitude = Number(form.longitude);
  const hasCoordinates =
    form.latitude.trim() !== '' &&
    form.longitude.trim() !== '' &&
    Number.isFinite(parsedLatitude) &&
    Number.isFinite(parsedLongitude);

  const handleSearchOnMap = async () => {
    if (!form.address.trim()) {
      void notifyWarning({
        title: 'Falta la dirección',
        text: 'Ingresá una dirección antes de buscarla en el mapa.',
      });
      return;
    }

    setGeocoding(true);
    const result = await geocodeAddress(form.address);
    setGeocoding(false);

    if (!result) {
      void notifyWarning({
        title: 'No se encontró la dirección',
        text: 'No pudimos ubicar esa dirección en el mapa. Podés ingresar las coordenadas manualmente.',
      });
      return;
    }

    onFieldChange('latitude', String(result.latitude));
    onFieldChange('longitude', String(result.longitude));
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
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
        <TextField
          label="Dirección"
          value={form.address}
          onChange={e => onFieldChange('address', e.target.value)}
          required
          fullWidth
        />
        <Button
          variant="outlined"
          onClick={() => void handleSearchOnMap()}
          disabled={geocoding}
          sx={{ whiteSpace: 'nowrap' }}
        >
          {geocoding ? 'Buscando…' : 'Buscar en el mapa'}
        </Button>
      </Stack>

      {hasCoordinates && (
        <OpenStreetMapEmbed
          latitude={parsedLatitude}
          longitude={parsedLongitude}
          title={form.name || 'la cancha'}
          height={240}
        />
      )}

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
