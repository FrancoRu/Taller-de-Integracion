import { IconButton, Stack, Typography } from '@mui/material';
import { AddIcon } from '@/views/core/MUI/icons/icons';
import { ZoneConfig, createEmptyZone } from '../types';
import ZoneEditor from './ZoneEditor';

interface DivisionesStepProps {
  zones: ZoneConfig[];
  onChange: (zones: ZoneConfig[]) => void;
}

export default function DivisionesStep({ zones, onChange }: DivisionesStepProps) {
  const updateZone = (zoneId: string, updates: Partial<ZoneConfig>) => {
    onChange(zones.map(zone => (zone.id === zoneId ? { ...zone, ...updates } : zone)));
  };

  const addZone = () => onChange([...zones, createEmptyZone()]);
  const removeZone = (zoneId: string) => onChange(zones.filter(zone => zone.id !== zoneId));

  return (
    <Stack spacing={3}>
      <Typography variant="body2" sx={{
        color: "text.secondary"
      }}>
        Cada zona tiene un nombre libre, una fase de grupos opcional y tantas copas paralelas como
        quieras (cada una con su propio nombre y formato por ronda). Los equipos se inscriben más
        adelante y se asignan a cada zona cuando cierra la inscripción.
      </Typography>

      {zones.map(zone => (
        <ZoneEditor
          key={zone.id}
          zone={zone}
          onChange={updates => updateZone(zone.id, updates)}
          onRemove={() => removeZone(zone.id)}
        />
      ))}

      <IconButton
        aria-label="Agregar zona"
        onClick={addZone}
        sx={{ alignSelf: 'flex-start', border: 1, borderColor: 'divider', borderRadius: 1, px: 2 }}
      >
        <AddIcon fontSize="small" sx={{ mr: 1 }} />
        <Typography variant="button">Nueva zona</Typography>
      </IconButton>
    </Stack>
  );
}
