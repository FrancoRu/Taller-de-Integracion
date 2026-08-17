import { Chip, Stack, Typography } from '@mui/material';
import { GUID } from '@/modules/core/types/types';
import { ITeamResponse } from '@/modules/team/type/team.d';

interface EquiposStepProps {
  availableTeams: ITeamResponse[];
  selectedTeamIds: GUID[];
  onChange: (teamIds: GUID[]) => void;
}

/**
 * Lets the admin pick which teams from the club's general roster (teams
 * not yet registered in any tournament) participate in this one.
 */
export default function EquiposStep({
  availableTeams,
  selectedTeamIds,
  onChange,
}: EquiposStepProps) {
  const toggle = (teamId: GUID) => {
    onChange(
      selectedTeamIds.includes(teamId)
        ? selectedTeamIds.filter(id => id !== teamId)
        : [...selectedTeamIds, teamId]
    );
  };

  if (availableTeams.length === 0) {
    return (
      <Typography sx={{
        color: "text.secondary"
      }}>No hay equipos disponibles en el padrón general (todos ya están inscriptos en otro torneo).
              </Typography>
    );
  }

  return (
    <Stack spacing={1.5}>
      <Typography variant="body2" sx={{
        color: "text.secondary"
      }}>
        Seleccioná los equipos que participan en este torneo ({selectedTeamIds.length} seleccionados).
      </Typography>
      <Stack
        direction="row"
        sx={{
          flexWrap: "wrap",
          gap: 1
        }}>
        {availableTeams.map(team => (
          <Chip
            key={team.id}
            label={team.name}
            color={selectedTeamIds.includes(team.id) ? 'primary' : 'default'}
            variant={selectedTeamIds.includes(team.id) ? 'filled' : 'outlined'}
            onClick={() => toggle(team.id)}
          />
        ))}
      </Stack>
    </Stack>
  );
}
