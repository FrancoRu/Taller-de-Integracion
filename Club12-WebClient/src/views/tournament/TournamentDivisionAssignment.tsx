import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Alert,
  AlertTitle,
  Box,
  Button,
  Checkbox,
  Chip,
  Collapse,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  IconButton,
  InputAdornment,
  List,
  ListItem,
  ListItemButton,
  ListItemText,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import SearchIcon from '@mui/icons-material/Search';
import AddIcon from '@mui/icons-material/Add';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import ExpandLessIcon from '@mui/icons-material/ExpandLess';
import { GUID } from '@/modules/core/types/types';
import { useTeam } from '@/modules/team/hook/team.hook';
import { useStage } from '@/modules/stage/hook/stage.hook';
import { useDivision } from '@/modules/division/hook/division.hook';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { StageType, IStageResponse } from '@/modules/stage/type/stage';
import { IDivisionResponse } from '@/modules/division/type/division';
import { ITeamResponse } from '@/modules/team/type/team.d';
import {
  ITournamentCompletability,
  ITournamentResponse,
} from '@/modules/tournament/type/tournament';
import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';
import { FILTER_OPTIONS_PAGE_SIZE } from '@/modules/core/constants/pagination';
import {
  confirmAction,
  notifyError,
  notifySuccess,
} from '@/modules/core/utils/confirmDialog';
import { completabilityIssueMessage } from '@/modules/tournament/utils/completabilityMessages';
import { DetailSkeleton } from '@/views/core/components/skeletons';
import TeamLogo from '@/views/core/components/TeamLogo';

interface TournamentDivisionAssignmentProps {
  tournament: ITournamentResponse;
}

/** One group stage of a division together with the teams already in it. */
interface StageGroup {
  stage: IStageResponse;
  assignedTeams: ITeamResponse[];
}

interface DivisionAssignment {
  division: IDivisionResponse;
  /**
   * The division's group stages. A regular zone has exactly one; a
   * cross-division cup (HU-110) fans out into N ("Grupo 1"…"Grupo N"). Empty
   * when the division has no group stage at all.
   */
  groups: StageGroup[];
}

/** The target of the team picker: a specific group stage and its eligible pool. */
interface PickerTarget {
  stage: IStageResponse;
  title: string;
  eligible: ITeamResponse[];
}

const TEAM_LOGO_SIZE = 32;

/** A compact team row (crest + name), reused in zones, the pool and the picker. */
function TeamIdentity({ team }: { team: ITeamResponse }) {
  return (
    <Stack direction="row" spacing={1} sx={{ alignItems: 'center', minWidth: 0 }}>
      <TeamLogo teamName={team.name} logoUrl={team.logoUrl} size={TEAM_LOGO_SIZE} />
      <Typography variant="body2" noWrap sx={{ minWidth: 0 }}>
        {team.name}
      </Typography>
    </Stack>
  );
}

/**
 * A searchable multi-select dialog to add teams to a zone. Shows each eligible
 * team as a card (crest + name) with a checkbox, filtered by a search box, and
 * confirms the whole selection in one call.
 */
function TeamPickerDialog({
  target,
  busy,
  onClose,
  onConfirm,
}: {
  target: PickerTarget | null;
  busy: boolean;
  onClose: () => void;
  onConfirm: (stage: IStageResponse, teamIds: GUID[]) => void;
}) {
  const [query, setQuery] = useState('');
  const [selected, setSelected] = useState<Set<GUID>>(new Set());

  // Reset the search and selection whenever a different picker target opens.
  useEffect(() => {
    setQuery('');
    setSelected(new Set());
  }, [target?.stage.id]);

  const filtered = useMemo(() => {
    if (!target) return [];
    const q = query.trim().toLowerCase();
    return q
      ? target.eligible.filter(team => team.name.toLowerCase().includes(q))
      : target.eligible;
  }, [target, query]);

  const toggle = (teamId: GUID) => {
    setSelected(prev => {
      const next = new Set(prev);
      if (next.has(teamId)) {
        next.delete(teamId);
      } else {
        next.add(teamId);
      }
      return next;
    });
  };

  return (
    <Dialog open={target !== null} onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle sx={{ pr: 6 }}>
        Agregar equipos{target ? ` · ${target.title}` : ''}
        <IconButton
          aria-label="Cerrar"
          onClick={onClose}
          sx={{ position: 'absolute', right: 8, top: 8 }}
        >
          <CloseIcon />
        </IconButton>
      </DialogTitle>
      <DialogContent dividers>
        <TextField
          value={query}
          onChange={e => setQuery(e.target.value)}
          placeholder="Buscar equipo…"
          size="small"
          fullWidth
          autoFocus
          sx={{ mb: 1.5 }}
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

        {filtered.length === 0 ? (
          <Typography variant="body2" sx={{ color: 'text.secondary', py: 2 }}>
            No hay equipos disponibles para agregar.
          </Typography>
        ) : (
          <List dense disablePadding sx={{ maxHeight: 320, overflowY: 'auto' }}>
            {filtered.map(team => (
              <ListItemButton
                key={team.id}
                onClick={() => toggle(team.id)}
                sx={{ borderRadius: 1 }}
              >
                <Checkbox
                  edge="start"
                  checked={selected.has(team.id)}
                  tabIndex={-1}
                  disableRipple
                  sx={{ mr: 0.5 }}
                />
                <TeamIdentity team={team} />
              </ListItemButton>
            ))}
          </List>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={busy}>
          Cancelar
        </Button>
        <Button
          variant="contained"
          disabled={busy || selected.size === 0 || !target}
          onClick={() => target && onConfirm(target.stage, [...selected])}
        >
          Agregar{selected.size > 0 ? ` (${selected.size})` : ''}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

/**
 * Division-assignment workspace (HU-108 + HU-109). Available as a DRAFT while
 * registration is open and once it closes: for every regular zone it shows the
 * teams already placed there (each removable) and a searchable "+ Agregar
 * equipos" picker for the enrolled-but-unassigned teams — enforcing "one team,
 * one zone" across regular zones while leaving the cross-division cup as a
 * parallel membership. Assign/remove update the view in place (no full reload).
 * A live completability panel gates the "Iniciar torneo" transition.
 */
const TournamentDivisionAssignment: React.FC<
  TournamentDivisionAssignmentProps
> = ({ tournament }) => {
  const { getTeamsByFiltered } = useTeam();
  const { getStagesByFilters, assignTeamsToStage, unassignTeamsFromStage } =
    useStage();
  const { getDivisionsByFilters } = useDivision();
  const { getCompletability, putTournamentById } = useTournament();

  const [loading, setLoading] = useState(false);
  const [assignments, setAssignments] = useState<DivisionAssignment[]>([]);
  const [enrolledTeams, setEnrolledTeams] = useState<ITeamResponse[]>([]);
  const [completability, setCompletability] =
    useState<ITournamentCompletability | null>(null);
  const [busy, setBusy] = useState(false);
  const [picker, setPicker] = useState<PickerTarget | null>(null);
  const [collapsed, setCollapsed] = useState<Set<GUID>>(new Set());

  const toggleCollapsed = (divisionId: GUID) => {
    setCollapsed(prev => {
      const next = new Set(prev);
      if (next.has(divisionId)) {
        next.delete(divisionId);
      } else {
        next.add(divisionId);
      }
      return next;
    });
  };

  // Draft assignment is allowed while registration is open and once it closes;
  // it is only unavailable once the tournament has already started.
  const canAssign =
    tournament.status === TournamentStatus.OpenForRegistration ||
    tournament.status === TournamentStatus.RegistrationClosed;
  const isRegistrationClosed =
    tournament.status === TournamentStatus.RegistrationClosed;

  useEffect(() => {
    if (!canAssign) {
      return;
    }

    let active = true;

    const load = async () => {
      setLoading(true);
      try {
        const [completabilityResult, divisionsResult, enrolledResult] =
          await Promise.all([
            getCompletability(tournament.id),
            getDivisionsByFilters({
              tournamentId: tournament.id,
              pageSize: FILTER_OPTIONS_PAGE_SIZE,
            }),
            getTeamsByFiltered({
              tournamentId: tournament.id,
              pageSize: FILTER_OPTIONS_PAGE_SIZE,
            }),
          ]);

        const divisions = divisionsResult?.items ?? [];

        const nextAssignments = await Promise.all(
          divisions.map(async division => {
            const stagesResult = await getStagesByFilters({
              divisionId: division.id,
              stageType: StageType.Group,
              pageSize: FILTER_OPTIONS_PAGE_SIZE,
            });

            const items = stagesResult?.items ?? [];
            const groupStages = items
              .filter(stage => stage.stageType === StageType.Group)
              .sort((a, b) => a.order - b.order);
            const resolvedStages = groupStages.length > 0 ? groupStages : items;

            const groups = await Promise.all(
              resolvedStages.map(async stage => {
                const assignedResult = await getTeamsByFiltered({
                  stageId: stage.id,
                  pageSize: FILTER_OPTIONS_PAGE_SIZE,
                });
                return { stage, assignedTeams: assignedResult?.items ?? [] };
              })
            );

            return { division, groups };
          })
        );

        if (!active) {
          return;
        }

        setAssignments(nextAssignments);
        setEnrolledTeams(enrolledResult?.items ?? []);
        setCompletability(completabilityResult ?? null);
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    };

    void load();

    return () => {
      active = false;
    };
  }, [
    canAssign,
    tournament.id,
    getCompletability,
    getDivisionsByFilters,
    getTeamsByFiltered,
    getStagesByFilters,
  ]);

  // Refetch ONLY the completability verdict after a change, so the start-gate
  // stays live without reloading the whole assignment view.
  const refreshCompletability = useCallback(async () => {
    const result = await getCompletability(tournament.id);
    setCompletability(result ?? null);
  }, [getCompletability, tournament.id]);

  const addTeamsLocally = (stageId: GUID, teams: ITeamResponse[]) => {
    setAssignments(prev =>
      prev.map(assignment => ({
        ...assignment,
        groups: assignment.groups.map(group =>
          group.stage.id === stageId
            ? {
                ...group,
                assignedTeams: [
                  ...group.assignedTeams,
                  ...teams.filter(
                    team => !group.assignedTeams.some(t => t.id === team.id)
                  ),
                ],
              }
            : group
        ),
      }))
    );
  };

  const removeTeamLocally = (stageId: GUID, teamId: GUID) => {
    setAssignments(prev =>
      prev.map(assignment => ({
        ...assignment,
        groups: assignment.groups.map(group =>
          group.stage.id === stageId
            ? {
                ...group,
                assignedTeams: group.assignedTeams.filter(t => t.id !== teamId),
              }
            : group
        ),
      }))
    );
  };

  const handleAdd = async (stage: IStageResponse, teamIds: GUID[]) => {
    const teams = teamIds
      .map(id => enrolledTeams.find(team => team.id === id))
      .filter((team): team is ITeamResponse => team !== undefined);

    setBusy(true);
    try {
      const ok = await assignTeamsToStage(stage.id, teamIds);
      if (ok) {
        addTeamsLocally(stage.id, teams);
        setPicker(null);
        await refreshCompletability();
      }
    } finally {
      setBusy(false);
    }
  };

  const handleRemove = async (stage: IStageResponse, team: ITeamResponse) => {
    setBusy(true);
    try {
      const ok = await unassignTeamsFromStage(stage.id, [team.id]);
      if (ok) {
        removeTeamLocally(stage.id, team.id);
        await refreshCompletability();
      }
    } finally {
      setBusy(false);
    }
  };

  const handleStart = async () => {
    const confirmed = await confirmAction({
      title: 'Iniciar torneo',
      text: 'Se cerrará la inscripción, se generará el fixture y comenzará el torneo. Esta acción no se puede revertir. ¿Continuar?',
      confirmButtonText: 'Iniciar torneo',
    });

    if (!confirmed) {
      return;
    }

    const base = {
      name: tournament.name,
      description: tournament.description,
      startDate: new Date(tournament.startDate),
      teamRegistrationDeadline: new Date(tournament.teamRegistrationDeadline),
    };

    setBusy(true);
    try {
      // The backend state machine requires RegistrationClosed before Ongoing.
      // When the organizer starts straight from an open-registration draft, we
      // close the registration first, then start — so there is no hidden
      // "cerrar inscripción" step to hunt for in the edit screen.
      if (tournament.status === TournamentStatus.OpenForRegistration) {
        const closed = await putTournamentById(tournament.id, {
          ...base,
          status: TournamentStatus.RegistrationClosed,
        });
        if (!closed) {
          await notifyError({
            title: 'No se pudo cerrar la inscripción',
            text: 'Volvé a intentar en unos segundos.',
          });
          return;
        }
      }

      const started = await putTournamentById(tournament.id, {
        ...base,
        status: TournamentStatus.Ongoing,
      });

      if (!started) {
        await notifyError({
          title: 'No se pudo iniciar el torneo',
          text: 'Puede ser un problema momentáneo al generar el fixture. Esperá unos segundos y volvé a intentar.',
        });
        return;
      }

      await refreshCompletability();
      await notifySuccess({
        title: 'Torneo iniciado',
        text: 'El fixture se generó y el torneo está en curso.',
      });
    } finally {
      setBusy(false);
    }
  };

  // "One team, one zone": every team already in a regular zone.
  const teamsInRegularZones = useMemo(() => {
    const set = new Set<GUID>();
    assignments.forEach(({ division, groups }) => {
      if (!division.isCrossDivisionCup) {
        groups.forEach(group =>
          group.assignedTeams.forEach(team => set.add(team.id))
        );
      }
    });
    return set;
  }, [assignments]);

  const unassignedTeams = useMemo(
    () => enrolledTeams.filter(team => !teamsInRegularZones.has(team.id)),
    [enrolledTeams, teamsInRegularZones]
  );

  // Teams eligible for a division's group. A regular zone bars any team already
  // placed in another regular zone; a cross-division cup (parallel membership)
  // only bars teams already in one of its own groups.
  const eligibleTeamsFor = (
    { division, groups }: DivisionAssignment,
    stageId: GUID
  ): ITeamResponse[] => {
    if (division.isCrossDivisionCup) {
      const alreadyInCup = new Set<GUID>();
      groups.forEach(group =>
        group.assignedTeams.forEach(team => alreadyInCup.add(team.id))
      );
      return enrolledTeams.filter(team => !alreadyInCup.has(team.id));
    }

    const group = groups.find(g => g.stage.id === stageId);
    const here = new Set((group?.assignedTeams ?? []).map(t => t.id));
    return enrolledTeams.filter(
      team => !teamsInRegularZones.has(team.id) || here.has(team.id)
    ).filter(team => !here.has(team.id));
  };

  if (!canAssign) {
    return (
      <Typography variant="body2" sx={{ color: 'text.secondary' }}>
        La asignación no está disponible una vez que el torneo comenzó.
      </Typography>
    );
  }

  if (loading) {
    return <DetailSkeleton />;
  }

  const issues = completability?.issues ?? [];
  const hasUnassigned = unassignedTeams.length > 0;
  // The start button independently requires that NO enrolled team is left
  // without a zone — a safety net over the backend completability so the
  // tournament can never start with unassigned teams / empty divisions.
  // Ready when every enrolled team has a zone and the backend agrees. Starting
  // from an open-registration draft is allowed — handleStart closes the
  // registration first before starting, so no separate step is needed.
  const readyToStart = (completability?.canStart ?? false) && !hasUnassigned;

  return (
    <Box sx={{ width: '100%' }}>
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={1}
        sx={{ alignItems: { sm: 'center' }, justifyContent: 'space-between', mb: 2 }}
      >
        <Typography variant="h6">Asignación de equipos a zonas</Typography>
        <Button
          variant="contained"
          onClick={() => void handleStart()}
          disabled={!readyToStart || busy}
        >
          Iniciar torneo
        </Button>
      </Stack>

      {!isRegistrationClosed && (
        <Alert severity="info" sx={{ mb: 2 }}>
          Podés ir asignando los equipos a sus zonas como borrador mientras la
          inscripción sigue abierta. Cuando estén todos asignados, «Iniciar
          torneo» cierra la inscripción y genera el fixture automáticamente.
        </Alert>
      )}

      {issues.length > 0 || hasUnassigned ? (
        <Alert severity="warning" sx={{ mb: 2 }}>
          <AlertTitle>El torneo todavía no puede iniciarse</AlertTitle>
          <List dense disablePadding>
            {hasUnassigned && (
              <ListItem disableGutters>
                <ListItemText
                  primary={`Hay ${unassignedTeams.length} equipo(s) sin zona asignada. Asignálos todos antes de iniciar.`}
                />
              </ListItem>
            )}
            {issues.map((issue, index) => (
              <ListItem key={`${issue.code}-${index}`} disableGutters>
                <ListItemText primary={completabilityIssueMessage(issue)} />
              </ListItem>
            ))}
          </List>
        </Alert>
      ) : (
        <Alert severity="success" sx={{ mb: 2 }}>
          Todos los equipos están asignados: el torneo está listo para iniciarse.
        </Alert>
      )}

      {/* Pool of enrolled teams not yet placed in any regular zone. */}
      <Box
        component="section"
        aria-label="Equipos sin zona"
        sx={{
          border: 1,
          borderColor: unassignedTeams.length > 0 ? 'warning.main' : 'divider',
          borderRadius: 1,
          p: 2,
          mb: 3,
        }}
      >
        <Typography variant="subtitle2" sx={{ mb: 1 }}>
          Equipos sin zona ({unassignedTeams.length})
        </Typography>
        {unassignedTeams.length === 0 ? (
          <Typography variant="body2" sx={{ color: 'text.secondary' }}>
            Todos los equipos inscriptos tienen una zona asignada.
          </Typography>
        ) : (
          <Stack direction="row" sx={{ flexWrap: 'wrap', gap: 1 }}>
            {unassignedTeams.map(team => (
              <Chip
                key={team.id}
                avatar={
                  <TeamLogo
                    teamName={team.name}
                    logoUrl={team.logoUrl}
                    size={24}
                  />
                }
                label={team.name}
                variant="outlined"
              />
            ))}
          </Stack>
        )}
      </Box>

      <Divider sx={{ mb: 3 }} />

      {assignments.length === 0 ? (
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          Este torneo no tiene divisiones para asignar.
        </Typography>
      ) : (
        <Stack spacing={3}>
          {assignments.map(assignment => {
            const { division, groups } = assignment;
            const showGroupHeadings = division.isCrossDivisionCup;
            const isCollapsed = collapsed.has(division.id);
            const divisionTeamCount = groups.reduce(
              (total, group) => total + group.assignedTeams.length,
              0
            );

            return (
              <Box
                key={division.id}
                component="section"
                aria-label={division.name}
                sx={{ border: 1, borderColor: 'divider', borderRadius: 1, p: 2 }}
              >
                <Stack
                  direction="row"
                  spacing={1}
                  onClick={() => toggleCollapsed(division.id)}
                  sx={{ alignItems: 'center', cursor: 'pointer', userSelect: 'none' }}
                >
                  <IconButton
                    size="small"
                    aria-label={isCollapsed ? 'Expandir' : 'Colapsar'}
                    aria-expanded={!isCollapsed}
                  >
                    {isCollapsed ? <ExpandMoreIcon /> : <ExpandLessIcon />}
                  </IconButton>
                  <Typography variant="subtitle1">{division.name}</Typography>
                  {division.isCrossDivisionCup && (
                    <Chip size="small" color="secondary" label="Copa cruzada" />
                  )}
                  <Chip
                    size="small"
                    variant="outlined"
                    label={`${divisionTeamCount} equipos`}
                  />
                </Stack>

                <Collapse in={!isCollapsed}>
                  <Box sx={{ pt: 1.5 }}>
                    {groups.length === 0 ? (
                      <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                        Esta división no tiene fase de grupos para asignar equipos.
                      </Typography>
                    ) : (
                      <Stack spacing={2}>
                        {groups.map(({ stage, assignedTeams }) => (
                      <Box
                        key={stage.id}
                        component="section"
                        aria-label={stage.name}
                      >
                        <Stack
                          direction="row"
                          sx={{
                            alignItems: 'center',
                            justifyContent: 'space-between',
                            mb: 1,
                          }}
                        >
                          <Typography variant="subtitle2" sx={{ color: 'text.secondary' }}>
                            {showGroupHeadings ? stage.name : 'Equipos'} (
                            {assignedTeams.length})
                          </Typography>
                          <Button
                            size="small"
                            variant="outlined"
                            startIcon={<AddIcon />}
                            disabled={busy}
                            onClick={() =>
                              setPicker({
                                stage,
                                title: showGroupHeadings
                                  ? `${division.name} · ${stage.name}`
                                  : division.name,
                                eligible: eligibleTeamsFor(assignment, stage.id),
                              })
                            }
                          >
                            Agregar equipos
                          </Button>
                        </Stack>

                        {assignedTeams.length === 0 ? (
                          <Typography
                            variant="body2"
                            sx={{ color: 'text.secondary' }}
                          >
                            Todavía no hay equipos en esta zona.
                          </Typography>
                        ) : (
                          <Stack spacing={0.5}>
                            {assignedTeams.map(team => (
                              <Stack
                                key={team.id}
                                direction="row"
                                sx={{
                                  alignItems: 'center',
                                  justifyContent: 'space-between',
                                  py: 0.5,
                                }}
                              >
                                <TeamIdentity team={team} />
                                <IconButton
                                  size="small"
                                  aria-label={`Quitar ${team.name}`}
                                  disabled={busy}
                                  onClick={() => void handleRemove(stage, team)}
                                >
                                  <CloseIcon fontSize="small" />
                                </IconButton>
                              </Stack>
                            ))}
                          </Stack>
                        )}
                      </Box>
                        ))}
                      </Stack>
                    )}
                  </Box>
                </Collapse>
              </Box>
            );
          })}
        </Stack>
      )}

      <TeamPickerDialog
        target={picker}
        busy={busy}
        onClose={() => setPicker(null)}
        onConfirm={(stage, teamIds) => void handleAdd(stage, teamIds)}
      />
    </Box>
  );
};

export default TournamentDivisionAssignment;
