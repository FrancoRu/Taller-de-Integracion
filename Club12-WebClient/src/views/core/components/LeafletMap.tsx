import { useEffect } from 'react';
import { Box } from '@mui/material';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { MapContainer, Marker, TileLayer, useMap, useMapEvents } from 'react-leaflet';
import markerIcon2x from 'leaflet/dist/images/marker-icon-2x.png';
import markerIcon from 'leaflet/dist/images/marker-icon.png';
import markerShadow from 'leaflet/dist/images/marker-shadow.png';

// Vite bundles Leaflet's default marker icon paths incorrectly out of the
// box; point them at the bundled asset URLs explicitly (a well-known
// Leaflet+bundler gotcha, not project-specific behavior).
const DEFAULT_ICON = L.icon({
  iconUrl: markerIcon,
  iconRetinaUrl: markerIcon2x,
  shadowUrl: markerShadow,
  iconSize: [25, 41],
  iconAnchor: [12, 41],
});

interface RecenterProps {
  latitude: number;
  longitude: number;
}

/** Keeps the map centered on the given point when it changes externally
 * (e.g. after a geocoded address), without fighting the user's own
 * pan/zoom in between updates. */
function Recenter({ latitude, longitude }: RecenterProps) {
  const map = useMap();
  useEffect(() => {
    map.setView([latitude, longitude], map.getZoom());
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [latitude, longitude]);
  return null;
}

interface ClickToPlaceProps {
  onLocationChange: (latitude: number, longitude: number) => void;
}

function ClickToPlace({ onLocationChange }: ClickToPlaceProps) {
  useMapEvents({
    click: event => onLocationChange(event.latlng.lat, event.latlng.lng),
  });
  return null;
}

interface LeafletMapProps {
  latitude: number;
  longitude: number;
  /** Accessible title for the map. */
  title: string;
  /** Map height in pixels. */
  height?: number;
  /** Initial zoom level; ignored on later recenters (the user's own zoom is preserved). */
  zoom?: number;
  /** When provided, the map is interactive: click anywhere or drag the pin to set a new point. Omitted in read-only contexts. */
  onLocationChange?: (latitude: number, longitude: number) => void;
}

/**
 * A free, no-API-key interactive map (Leaflet + OpenStreetMap tiles) that
 * supports normal pan/zoom always, and — when `onLocationChange` is given —
 * lets the user click anywhere or drag the marker to place a pin. Reusable
 * wherever a venue's coordinates need to be shown or picked.
 */
export default function LeafletMap({
  latitude,
  longitude,
  title,
  height = 320,
  zoom = 15,
  onLocationChange,
}: LeafletMapProps) {
  return (
    <Box
      role="group"
      aria-label={`Mapa de ${title}`}
      sx={{
        width: '100%',
        height,
        maxWidth: 640,
        borderRadius: 2,
        overflow: 'hidden',
        border: '1px solid',
        borderColor: 'divider',
        '& .leaflet-container': { width: '100%', height: '100%' },
      }}
    >
      <MapContainer
        center={[latitude, longitude]}
        zoom={zoom}
        // Read-only display (no onLocationChange, e.g. a venue's detail
        // page) is a static view of the pin — zoom/pan would just invite
        // the admin to drag it around a map that has nothing else to show.
        // The interactive picker keeps full pan/zoom/click-to-place.
        scrollWheelZoom={Boolean(onLocationChange)}
        zoomControl={Boolean(onLocationChange)}
        doubleClickZoom={Boolean(onLocationChange)}
        touchZoom={Boolean(onLocationChange)}
        boxZoom={Boolean(onLocationChange)}
        dragging={Boolean(onLocationChange)}
        keyboard={Boolean(onLocationChange)}
      >
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        <Marker
          position={[latitude, longitude]}
          icon={DEFAULT_ICON}
          draggable={Boolean(onLocationChange)}
          eventHandlers={
            onLocationChange
              ? {
                  dragend: event => {
                    const marker = event.target as L.Marker;
                    const { lat, lng } = marker.getLatLng();
                    onLocationChange(lat, lng);
                  },
                }
              : undefined
          }
        />
        <Recenter latitude={latitude} longitude={longitude} />
        {onLocationChange && <ClickToPlace onLocationChange={onLocationChange} />}
      </MapContainer>
    </Box>
  );
}
