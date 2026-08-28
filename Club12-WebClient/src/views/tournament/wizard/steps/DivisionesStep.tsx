import { useMemo } from 'react';
import {
  Alert,
  Box,
  Chip,
  Divider,
  FormControlLabel,
  IconButton,
  MenuItem,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material';
import { AddIcon, DeleteIcon } from '@/views/core/MUI/icons/icons';
import { GUID } from '@/modules/core/types/types';
import { ITeamResponse } from '@/modules/team/type/team.d';
import {
  CupConfig,
  PlayoffMappingConfig,
  ROUND_ROBIN_LEGS_OPTIONS,
  ZoneConfig,
  createEmptyZone,
} from '../types';
import CupsEditor from './CupsEditor';
import PlayoffRangesEditor from './PlayoffRangesEditor';

interface DivisionesStepProps {
  teams: ITeamResponse[];
  zones: ZoneConfig[];
  onChange: (zones: ZoneConfig[]) => void;
}

/** Toggles a team's membership in one zone, removing it from every other zone — a team can only ever be in one zone at a time. */
const toggleTeamInZone = (zones: ZoneConfig[], zoneId: string, teamId: GUID): ZoneConfig[] =>
  zones.map(zone => {
    if (zone.id === zoneId) {
      const alreadyIn = zone.teamIds.includes(teamId);
      return {
        ...zone,
        teamIds: alreadyIn
          ? zone.teamIds.filter(id => id !== teamId)
          : [...zone.teamIds, teamId],
      };
    }

    return { ...zone, teamIds: zone.teamIds.filter(id => id !== teamId) };
  });

export default function DivisionesStep({ teams, zones, onChange }: DivisionesStepProps) {
  const updateZone = (zoneId: string, updates: Partial<ZoneConfig>) => {
    onChange(zones.map(zone => (zone.id === zoneId ? { ...zone, ...updates } : zone)));
  };

  const addZone = () => onChange([...zones, createEmptyZone()]);
  const removeZone = (zoneId: string) => onChange(zones.filter(zone => zone.id !== zoneId));

  const teamName = (teamId: GUID): string =>
    teams.find(team => team.id === teamId)?.name ?? teamId;

  /** The zone each team currently belongs to, if any — a team is in at most one zone at a time. */
  const zoneByTeamId = useMemo(() => {
    const map = new Map<GUID, ZoneConfig>();
    zones.forEach(zone => {
      zone.teamIds.forEach(teamId => map.set(teamId, zone));
    });
    return map;
  }, [zones]);

  const unassignedTeams = useMemo(
    () => teams.filter(team => !zoneByTeamId.has(team.id)),
    [teams, zoneByTeamId]
  );

  return (
    <Stack spacing={3}>
      <Typography variant="body2" sx={{
        color: "text.secondary"
      }}>
        Cada zona tiene un nombre libre, sus equipos, una fase de grupos opcional y tantas copas
        paralelas como quieras (cada una con su propio nombre y formato por ronda). Hacé clic en
        un equipo para asignarlo a una zona: si ya estaba en otra, se mueve automáticamente.
      </Typography>

      {zones.length > 0 && (
        <Alert severity={unassignedTeams.length > 0 ? 'warning' : 'success'}>
          {unassignedTeams.length > 0
            ? `${unassignedTeams.length} equipo(s) todavía sin zona: ${unassignedTeams
                .map(team => team.name)
                .join(', ')}.`
            : 'Todos los equipos inscriptos ya tienen una zona asignada.'}
        </Alert>
      )}

      {zones.map(zone => (
        <Box key={zone.id} sx={{ border: 1, borderColor: 'divider', borderRadius: 1, p: 2 }}>
          <Stack
            direction="row"
            spacing={1}
            sx={{
              alignItems: "center",
              mb: 1.5
            }}>
            <TextField
              label="Nombre de la zona (libre)"
              size="small"
              value={zone.name}
              onChange={e => updateZone(zone.id, { name: e.target.value })}
              fullWidth
            />
            <IconButton aria-label="Eliminar zona" color="error" onClick={() => removeZone(zone.id)}>
              <DeleteIcon fontSize="small" />
            </IconButton>
          </Stack>

          <Typography variant="caption" sx={{
            color: "text.secondary"
          }}>
            Equipos de esta zona ({zone.teamIds.length}) — clic para agregar/quitar, o para
            traer un equipo desde otra zona
          </Typography>
          <Stack
            direction="row"
            sx={{
              flexWrap: "wrap",
              gap: 1,
              mb: 2,
              mt: 0.5
            }}>
            {teams.map(team => {
              const inThisZone = zone.teamIds.includes(team.id);
              const otherZone = !inThisZone ? zoneByTeamId.get(team.id) : undefined;
              const isInOtherZone = Boolean(otherZone && otherZone.id !== zone.id);

              return (
                <Chip
                  key={team.id}
                  label={
                    isInOtherZone
                      ? `${teamName(team.id)} · en "${otherZone!.name || '(sin nombre)'}"`
                      : teamName(team.id)
                  }
                  color={inThisZone ? 'primary' : isInOtherZone ? 'warning' : 'default'}
                  variant={inThisZone ? 'filled' : 'outlined'}
                  onClick={() => onChange(toggleTeamInZone(zones, zone.id, team.id))}
                />
              );
            })}
          </Stack>

          <Stack
            direction="row"
            spacing={2}
            sx={{
              alignItems: "center",
              mb: 2
            }}>
            <FormControlLabel
              control={
                <Switch
                  checked={zone.hasGroupStage}
                  onChange={e => updateZone(zone.id, { hasGroupStage: e.target.checked })}
                />
              }
              label="Fase de grupos"
            />
            {zone.hasGroupStage && (
              <TextField
                select
                size="small"
                label="Veces que se enfrenta cada par"
                value={zone.roundRobinLegs}
                onChange={e => updateZone(zone.id, { roundRobinLegs: Number(e.target.value) })}
                sx={{ minWidth: 220 }}
              >
                {ROUND_ROBIN_LEGS_OPTIONS.map(option => (
                  <MenuItem key={option} value={option}>
                    {option === 1 ? 'Una vez (simple)' : `${option} veces`}
                  </MenuItem>
                ))}
              </TextField>
            )}
          </Stack>

          {/* Per-division scoring (HU-79): defaults 2/1, no draw points. */}
          <Stack
            direction="row"
            spacing={2}
            sx={{
              alignItems: 'center',
              mb: 2,
            }}>
            <TextField
              type="number"
              size="small"
              label="Puntos por victoria"
              value={zone.pointsForWin}
              onChange={e => updateZone(zone.id, { pointsForWin: Number(e.target.value) })}
              slotProps={{ htmlInput: { min: 0 } }}
              sx={{ width: 180 }}
            />
            <TextField
              type="number"
              size="small"
              label="Puntos por derrota"
              value={zone.pointsForLoss}
              onChange={e => updateZone(zone.id, { pointsForLoss: Number(e.target.value) })}
              slotProps={{ htmlInput: { min: 0 } }}
              sx={{ width: 180 }}
            />
          </Stack>

          <Divider sx={{ mb: 2 }} />

          <Typography variant="subtitle2" sx={{
            mb: 1
          }}>
            Playoffs de {zone.name || 'esta zona'}
          </Typography>
          <CupsEditor
            cups={zone.cups}
            onChange={(cups: CupConfig[]) => updateZone(zone.id, { cups })}
          />

          <Typography variant="subtitle2" sx={{ mb: 1, mt: 2 }}>
            Clasificación a playoffs por rango
          </Typography>
          <PlayoffRangesEditor
            mappings={zone.playoffMappings}
            cups={zone.cups}
            teamCount={zone.teamIds.length}
            onChange={(playoffMappings: PlayoffMappingConfig[]) =>
              updateZone(zone.id, { playoffMappings })
            }
          />
        </Box>
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
