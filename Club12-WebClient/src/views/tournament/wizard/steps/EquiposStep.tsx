import { useMemo, useState } from 'react';
import { Chip, InputAdornment, Stack, TextField, Typography } from '@mui/material';
import { SearchIcon } from '@/views/core/MUI/icons/icons';
import { GUID } from '@/modules/core/types/types';
import { ITeamResponse } from '@/modules/team/type/team.d';

interface EquiposStepProps {
  availableTeams: ITeamResponse[];
  /** Tournament name by id, used to flag teams currently playing elsewhere. */
  tournamentNameById?: Map<string, string>;
  selectedTeamIds: GUID[];
  onChange: (teamIds: GUID[]) => void;
}

/** A team plus the name of the tournament it's currently playing in, if any. */
interface TeamWithCurrentTournament {
  team: ITeamResponse;
  currentTournamentName?: string;
}

const byNameAlphabetically = (a: TeamWithCurrentTournament, b: TeamWithCurrentTournament): number =>
  a.team.name.localeCompare(b.team.name, 'es', { sensitivity: 'base' });

/**
 * Lets the admin pick which teams from the club's general roster participate
 * in this tournament. A team already registered in another tournament is
 * still selectable — that's the normal way a team moves from one season to
 * the next — but it's flagged with its current tournament name so the
 * reassignment is a knowing choice, not a surprise.
 *
 * With dozens of club teams a flat, unsorted chip list stops being usable,
 * so the roster is filterable by name and split into two clearly labeled,
 * alphabetically sorted groups: teams free to join (no current tournament)
 * first, then teams that would be reassigned from another tournament.
 */
export default function EquiposStep({
  availableTeams,
  tournamentNameById,
  selectedTeamIds,
  onChange,
}: EquiposStepProps) {
  const [search, setSearch] = useState('');

  const toggle = (teamId: GUID) => {
    onChange(
      selectedTeamIds.includes(teamId)
        ? selectedTeamIds.filter(id => id !== teamId)
        : [...selectedTeamIds, teamId]
    );
  };

  const teamsWithTournament = useMemo<TeamWithCurrentTournament[]>(
    () =>
      availableTeams.map(team => ({
        team,
        currentTournamentName: team.tournamentId
          ? tournamentNameById?.get(team.tournamentId)
          : undefined,
      })),
    [availableTeams, tournamentNameById]
  );

  const filteredTeams = useMemo(() => {
    const normalizedSearch = search.trim().toLowerCase();
    if (!normalizedSearch) {
      return teamsWithTournament;
    }

    return teamsWithTournament.filter(({ team }) =>
      team.name.toLowerCase().includes(normalizedSearch)
    );
  }, [teamsWithTournament, search]);

  const freeTeams = useMemo(
    () => filteredTeams.filter(entry => !entry.currentTournamentName).sort(byNameAlphabetically),
    [filteredTeams]
  );

  const reassignableTeams = useMemo(
    () => filteredTeams.filter(entry => entry.currentTournamentName).sort(byNameAlphabetically),
    [filteredTeams]
  );

  const renderTeamChip = ({ team, currentTournamentName }: TeamWithCurrentTournament) => {
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

      <TextField
        label="Buscar equipo"
        size="small"
        value={search}
        onChange={e => setSearch(e.target.value)}
        slotProps={{
          input: {
            startAdornment: (
              <InputAdornment position="start">
                <SearchIcon fontSize="small" />
              </InputAdornment>
            ),
          },
        }}
      />

      {filteredTeams.length === 0 ? (
        <Typography variant="body2" sx={{
          color: "text.secondary"
        }}>
          Ningún equipo coincide con "{search}".
        </Typography>
      ) : (
        <>
          {freeTeams.length > 0 && (
            <Stack spacing={0.75}>
              <Typography variant="caption" sx={{
                color: "text.secondary",
                fontWeight: 600
              }}>
                Equipos disponibles ({freeTeams.length})
              </Typography>
              <Stack direction="row" sx={{ flexWrap: "wrap", gap: 1 }}>
                {freeTeams.map(renderTeamChip)}
              </Stack>
            </Stack>
          )}

          {reassignableTeams.length > 0 && (
            <Stack spacing={0.75}>
              <Typography variant="caption" sx={{
                color: "text.secondary",
                fontWeight: 600
              }}>
                Equipos ya inscriptos en otro torneo ({reassignableTeams.length})
              </Typography>
              <Stack direction="row" sx={{ flexWrap: "wrap", gap: 1 }}>
                {reassignableTeams.map(renderTeamChip)}
              </Stack>
            </Stack>
          )}
        </>
      )}
    </Stack>
  );
}
