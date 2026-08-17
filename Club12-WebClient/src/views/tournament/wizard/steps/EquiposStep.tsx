import { Chip, Stack, Typography } from '@mui/material';
import { GUID } from '@/modules/core/types/types';
import { ITeamResponse } from '@/modules/team/type/team.d';

interface EquiposStepProps {
  availableTeams: ITeamResponse[];
  /** Tournament name by id, used to flag teams currently playing elsewhere. */
  tournamentNameById?: Map<string, string>;
  selectedTeamIds: GUID[];
  onChange: (teamIds: GUID[]) => void;
}

/**
 * Lets the admin pick which teams from the club's general roster participate
 * in this tournament. A team already registered in another tournament is
 * still selectable — that's the normal way a team moves from one season to
 * the next — but it's flagged with its current tournament name so the
 * reassignment is a knowing choice, not a surprise.
 */
export default function EquiposStep({
  availableTeams,
  tournamentNameById,
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
      }}>No hay equipos cargados en el padrón general todavía.
              </Typography>
    );
  }

  return (
    <Stack spacing={1.5}>
      <Typography variant="body2" sx={{
        color: "text.secondary"
      }}>
        Seleccioná los equipos que participan en este torneo ({selectedTeamIds.length} seleccionados).
        Un equipo ya inscripto en otro torneo puede elegirse igual: pasa a jugar este torneo.
      </Typography>
      <Stack
        direction="row"
        sx={{
          flexWrap: "wrap",
          gap: 1
        }}>
        {availableTeams.map(team => {
          const currentTournamentName = team.tournamentId
            ? tournamentNameById?.get(team.tournamentId)
            : undefined;
          const isSelected = selectedTeamIds.includes(team.id);
          return (
            <Chip
              key={team.id}
              label={currentTournamentName ? `${team.name} (${currentTournamentName})` : team.name}
              color={isSelected ? 'primary' : currentTournamentName ? 'warning' : 'default'}
              variant={isSelected ? 'filled' : 'outlined'}
              onClick={() => toggle(team.id)}
            />
          );
        })}
      </Stack>
    </Stack>
  );
}
