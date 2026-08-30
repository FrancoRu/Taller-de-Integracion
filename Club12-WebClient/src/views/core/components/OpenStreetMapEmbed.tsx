import { Box, Button, Stack } from '@mui/material';

interface OpenStreetMapEmbedProps {
  latitude: number;
  longitude: number;
  /** Accessible title for the embedded map (e.g. the venue name). */
  title: string;
  /** How far around the point the viewport spans, in degrees. Smaller = closer. */
  spanDegrees?: number;
  /** Map height in pixels. */
  height?: number;
}

/**
 * A free, no-API-key map for a single point, using OpenStreetMap's public embed
 * (an <iframe>, so no map library or script is loaded) plus a "Ver en el mapa"
 * link that opens the location on openstreetmap.org. Reusable wherever a venue's
 * coordinates need to be shown.
 */
export default function OpenStreetMapEmbed({
  latitude,
  longitude,
  title,
  spanDegrees = 0.008,
  height = 320,
}: OpenStreetMapEmbedProps) {
  const west = longitude - spanDegrees;
  const east = longitude + spanDegrees;
  const south = latitude - spanDegrees;
  const north = latitude + spanDegrees;

  const embedSrc =
    `https://www.openstreetmap.org/export/embed.html` +
    `?bbox=${west}%2C${south}%2C${east}%2C${north}` +
    `&layer=mapnik&marker=${latitude}%2C${longitude}`;

  const viewHref =
    `https://www.openstreetmap.org/?mlat=${latitude}&mlon=${longitude}` +
    `#map=16/${latitude}/${longitude}`;

  return (
    <Stack spacing={1} sx={{ maxWidth: 640 }}>
      <Box
        component="iframe"
        title={`Mapa de ${title}`}
        src={embedSrc}
        loading="lazy"
        referrerPolicy="no-referrer-when-downgrade"
        sx={{
          width: '100%',
          height,
          border: '1px solid',
          borderColor: 'divider',
          borderRadius: 2,
        }}
      />
      <Button
        variant="text"
        component="a"
        href={viewHref}
        target="_blank"
        rel="noopener noreferrer"
        sx={{ alignSelf: 'flex-start' }}
      >
        Ver en el mapa
      </Button>
    </Stack>
  );
}
