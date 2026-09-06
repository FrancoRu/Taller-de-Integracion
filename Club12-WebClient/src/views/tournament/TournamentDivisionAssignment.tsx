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
  Menu,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import SearchIcon from '@mui/icons-material/Search';
import AddIcon from '@mui/icons-material/Add';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import ExpandLessIcon from '@mui/icons-material/ExpandLess';
import AutorenewIcon from '@mui/icons-material/Autorenew';
import CasinoIcon from '@mui/icons-material/Casino';
import TuneIcon from '@mui/icons-material/Tune';
import SwapHorizIcon from '@mui/icons-material/SwapHoriz';
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
} from '@/modules/core/utils/confirmDialog';
import { completabilityIssueMessage } from '@/modules/tournament/utils/completabilityMessages';
import {
  clearBlockingMessage,
  setBlockingMessage,
} from '@/modules/core/utils/requestActivity';
import { formatDateAr } from '@/modules/core/utils/formatDate';
import { DetailSkeleton } from '@/views/core/components/skeletons';
import TeamLogo from '@/views/core/components/TeamLogo';
import FieldInfoTooltip from '@/views/core/components/FieldInfoTooltip';
import PlayoffDrawDialog from '@/views/playoff/PlayoffDrawDialog';

interface TournamentDivisionAssignmentProps {
  tournament: ITournamentResponse;
}

/** One sub-group stage of a division together with the teams already placed in it. */
interface StageGroup {
  stage: IStageResponse;
  assignedTeams: ITeamResponse[];
}

interface DivisionAssignment {
  division: IDivisionResponse;
  /**
   * Every team currently enrolled in the division's roster
   * (`DivisionTeamRegistration`), independent of any stage placement. The
   * authoritative "who is in this division" fact — always populated, even
   * for a playoffs-only division with no group stage (this is the bug fix:
   * the old component derived enrollment from group stages, so a groupless
   * division rendered no widget at all).
   */
  roster: ITeamResponse[];
  /**
   * The division's sub-group stages (HU-121). A regular zone with
   * `subGroupCount` 1 has exactly one; a multi-sub-group zone or a
   * cross-division cup (HU-110) has N ("Grupo A"…"Grupo N" / "Grupo 1"…
   * "Grupo N"). Empty for a groupless (playoffs-only) division.
   */
  groups: StageGroup[];
  /**
   * The division's first (lowest-order) elimination stage — the playoff
   * draw target for a groupless division. Null when the division has no
   * elimination stage configured at all yet.
   */
  firstRoundStage: IStageResponse | null;
  /** Every team currently placed in ANY of the division's stages (group or bracket) — drives the unenroll cascade-confirm. */
  placedTeamIds: Set<GUID>;
}

/**
 * What the shared team picker dialog is currently open for: enrolling teams
 * into a division's roster, or placing already-enrolled roster teams into a
 * specific sub-group stage.
 */
type PickerRequest =
  | { kind: 'roster'; division: IDivisionResponse; title: string; eligible: ITeamResponse[] }
  | { kind: 'stage'; stage: IStageResponse; title: string; eligible: ITeamResponse[] };

const TEAM_LOGO_SIZE = 32;

/** A compact team row (crest + name), reused in the roster, sub-groups and the picker. */
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
 * A searchable multi-select dialog that adds teams — either enrolling them
 * into a division's roster, or placing already-enrolled teams into one of
 * its sub-groups, depending on what `target` was opened for. Shows each
 * eligible team as a row with a checkbox, filtered by a search box, and
 * confirms the whole selection in one call.
 */
function TeamPickerDialog({
  target,
  busy,
  onClose,
  onConfirm,
}: {
  target: PickerRequest | null;
  busy: boolean;
  onClose: () => void;
  onConfirm: (teamIds: GUID[]) => void;
}) {
  const [query, setQuery] = useState('');
  const [selected, setSelected] = useState<Set<GUID>>(new Set());

  const resetKey = target
    ? target.kind === 'roster'
      ? `roster:${target.division.id}`
      : `stage:${target.stage.id}`
    : null;

  // Reset the search and selection whenever a different picker target opens.
  useEffect(() => {
    setQuery('');
    setSelected(new Set());
  }, [resetKey]);

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
          onClick={() => onConfirm([...selected])}
        >
          Agregar{selected.size > 0 ? ` (${selected.size})` : ''}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

/**
 * HU-123: lets an admin change a division's sub-group count. The roster is
 * never touched by this — only the sub-group stage layer is rebuilt and
 * re-balanced — so the dialog states that plainly instead of a silent
 * destructive-sounding action.
 */
function RebuildSubGroupsDialog({
  division,
  currentCount,
  busy,
  onClose,
  onConfirm,
}: {
  division: IDivisionResponse | null;
  currentCount: number;
  busy: boolean;
  onClose: () => void;
  onConfirm: (subGroupCount: number) => void;
}) {
  const [count, setCount] = useState(currentCount);

  useEffect(() => {
    setCount(currentCount);
  }, [division?.id, currentCount]);

  return (
    <Dialog open={division !== null} onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle sx={{ pr: 6 }}>
        Editar cantidad de sub-grupos
        <IconButton
          aria-label="Cerrar"
          onClick={onClose}
          sx={{ position: 'absolute', right: 8, top: 8 }}
        >
          <CloseIcon />
        </IconButton>
      </DialogTitle>
      <DialogContent dividers>
        <Typography variant="body2" sx={{ color: 'text.secondary', mb: 2 }}>
          Los equipos inscriptos en {division?.name ?? 'la división'} no se tocan — solo se
          reconstruyen los sub-grupos y se reparten de nuevo, balanceados, entre la nueva
          cantidad.
        </Typography>
        <TextField
          type="number"
          size="small"
          label="Cantidad de sub-grupos"
          value={count}
          onChange={e => setCount(Number(e.target.value))}
          slotProps={{ htmlInput: { min: 1 } }}
          fullWidth
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={busy}>
          Cancelar
        </Button>
        <Button
          variant="contained"
          disabled={busy || !Number.isInteger(count) || count < 1}
          onClick={() => onConfirm(count)}
        >
          Confirmar
        </Button>
      </DialogActions>
    </Dialog>
  );
}

/** Every team placed in any of a division's group stages (union across sub-groups). */
const placedInAnyGroup = (groups: StageGroup[]): Set<GUID> =>
  new Set(groups.flatMap(group => group.assignedTeams.map(team => team.id)));

/**
 * Division-assignment workspace (HU-107/108/109, HU-121/122/123, HU-128).
 * Available as a DRAFT while registration is open and once it closes. Every
 * division ALWAYS shows a roster panel to enrol/unenrol teams — including a
 * playoffs-only division with no group stage, which used to render nothing
 * at all (the dead `groupStages.length > 0 ? groupStages : items` fallback).
 * A division with sub-groups additionally shows the per-sub-group placement
 * picker plus an "Auto-repartir" balanced distribution action; a groupless
 * division shows the playoff draw trigger instead. A live completability
 * panel gates the "Iniciar torneo" transition.
 */
const TournamentDivisionAssignment: React.FC<
  TournamentDivisionAssignmentProps
> = ({ tournament }) => {
  const { getTeamsByFiltered } = useTeam();
  const { getStagesByFilters, assignTeamsToStage, unassignTeamsFromStage } =
    useStage();
  const {
    getDivisionsByFilters,
    getRoster,
    enrollTeams,
    unenrollTeams,
    autoDistribute,
    rebuildSubGroups,
    reassignTeamToSubGroup,
  } = useDivision();
  const { getCompletability, putTournamentById } = useTournament();

  const [loading, setLoading] = useState(false);
  const [assignments, setAssignments] = useState<DivisionAssignment[]>([]);
  const [enrolledTeams, setEnrolledTeams] = useState<ITeamResponse[]>([]);
  const [completability, setCompletability] =
    useState<ITournamentCompletability | null>(null);
  const [busy, setBusy] = useState(false);
  const [picker, setPicker] = useState<PickerRequest | null>(null);
  const [rebuildTarget, setRebuildTarget] = useState<IDivisionResponse | null>(null);
  const [drawTarget, setDrawTarget] = useState<DivisionAssignment | null>(null);
  const [reassignMenu, setReassignMenu] = useState<{
    anchorEl: HTMLElement;
    division: IDivisionResponse;
    fromStage: IStageResponse;
    team: ITeamResponse;
  } | null>(null);
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

  const loadDivisionAssignment = useCallback(
    async (division: IDivisionResponse): Promise<DivisionAssignment> => {
      const [roster, stagesResult] = await Promise.all([
        getRoster(division.id),
        getStagesByFilters({
          divisionId: division.id,
          pageSize: FILTER_OPTIONS_PAGE_SIZE,
        }),
      ]);

      const allStages = stagesResult?.items ?? [];
      const groupStages = allStages
        .filter(stage => stage.stageType === StageType.Group)
        .sort((a, b) => a.order - b.order);
      const eliminationStages = allStages
        .filter(stage => stage.isElimination)
        .sort((a, b) => a.order - b.order);
      const firstRoundStage = eliminationStages[0] ?? null;

      const groups = await Promise.all(
        groupStages.map(async stage => {
          const assignedResult = await getTeamsByFiltered({
            stageId: stage.id,
            pageSize: FILTER_OPTIONS_PAGE_SIZE,
          });
          return { stage, assignedTeams: assignedResult?.items ?? [] };
        })
      );

      const eliminationAssigned = await Promise.all(
        eliminationStages.map(async stage => {
          const assignedResult = await getTeamsByFiltered({
            stageId: stage.id,
            pageSize: FILTER_OPTIONS_PAGE_SIZE,
          });
          return assignedResult?.items ?? [];
        })
      );

      const placedTeamIds = new Set<GUID>([
        ...groups.flatMap(group => group.assignedTeams.map(team => team.id)),
        ...eliminationAssigned.flat().map(team => team.id),
      ]);

      return {
        division,
        roster: roster ?? [],
        groups,
        firstRoundStage,
        placedTeamIds,
      };
    },
    [getRoster, getStagesByFilters, getTeamsByFiltered]
  );

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
          divisions.map(loadDivisionAssignment)
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
    loadDivisionAssignment,
  ]);

  // Refetch ONLY the completability verdict after a change, so the start-gate
  // stays live without reloading the whole assignment view.
  const refreshCompletability = useCallback(async () => {
    const result = await getCompletability(tournament.id);
    setCompletability(result ?? null);
  }, [getCompletability, tournament.id]);

  // Reloads a single division's roster/groups/bracket info after a rebuild,
  // auto-distribute or draw commit — those change more than one local field
  // at once, so a targeted refetch is simpler and safer than patching state.
  const reloadDivision = useCallback(
    async (division: IDivisionResponse) => {
      const next = await loadDivisionAssignment(division);
      setAssignments(prev =>
        prev.map(a => (a.division.id === division.id ? next : a))
      );
    },
    [loadDivisionAssignment]
  );

  const addTeamsToGroupLocally = (stage: IStageResponse, teams: ITeamResponse[]) => {
    setAssignments(prev =>
      prev.map(assignment => {
        if (assignment.division.id !== stage.divisionId) return assignment;

        const groups = assignment.groups.map(group =>
          group.stage.id === stage.id
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
        );

        const placedTeamIds = new Set(assignment.placedTeamIds);
        teams.forEach(team => placedTeamIds.add(team.id));

        return { ...assignment, groups, placedTeamIds };
      })
    );
  };

  const removeTeamFromGroupLocally = (stage: IStageResponse, teamId: GUID) => {
    setAssignments(prev =>
      prev.map(assignment => {
        if (assignment.division.id !== stage.divisionId) return assignment;

        const groups = assignment.groups.map(group =>
          group.stage.id === stage.id
            ? {
                ...group,
                assignedTeams: group.assignedTeams.filter(t => t.id !== teamId),
              }
            : group
        );

        const stillPlaced = groups.some(group =>
          group.assignedTeams.some(t => t.id === teamId)
        );
        const placedTeamIds = new Set(assignment.placedTeamIds);
        if (!stillPlaced) {
          placedTeamIds.delete(teamId);
        }

        return { ...assignment, groups, placedTeamIds };
      })
    );
  };

  const handleEnroll = async (division: IDivisionResponse, teamIds: GUID[]) => {
    const teams = teamIds
      .map(id => enrolledTeams.find(team => team.id === id))
      .filter((team): team is ITeamResponse => team !== undefined);

    setBusy(true);
    try {
      const ok = await enrollTeams(division.id, teamIds);
      if (ok) {
        setAssignments(prev =>
          prev.map(assignment =>
            assignment.division.id === division.id
              ? {
                  ...assignment,
                  roster: [
                    ...assignment.roster,
                    ...teams.filter(
                      team => !assignment.roster.some(r => r.id === team.id)
                    ),
                  ],
                }
              : assignment
          )
        );
        setPicker(null);
        await refreshCompletability();
      }
    } finally {
      setBusy(false);
    }
  };

  const handleUnenroll = async (
    assignment: DivisionAssignment,
    team: ITeamResponse
  ) => {
    // D7 / cascade-with-confirmation: unenrolling a team still placed in a
    // group or bracket slot removes that placement too — warn first. An
    // unplaced team is removed immediately, no dialog.
    if (assignment.placedTeamIds.has(team.id)) {
      const confirmed = await confirmAction({
        title: 'Quitar equipo de la división',
        text: `${team.name} está ubicado en un grupo o llave de ${assignment.division.name}. Esta acción también lo va a sacar de ese lugar.`,
        confirmButtonText: 'Sí, quitar',
      });
      if (!confirmed) {
        return;
      }
    }

    setBusy(true);
    try {
      const ok = await unenrollTeams(assignment.division.id, [team.id]);
      if (ok) {
        setAssignments(prev =>
          prev.map(a => {
            if (a.division.id !== assignment.division.id) return a;

            const groups = a.groups.map(group => ({
              ...group,
              assignedTeams: group.assignedTeams.filter(t => t.id !== team.id),
            }));
            const placedTeamIds = new Set(a.placedTeamIds);
            placedTeamIds.delete(team.id);

            return {
              ...a,
              roster: a.roster.filter(t => t.id !== team.id),
              groups,
              placedTeamIds,
            };
          })
        );
        await refreshCompletability();
      }
    } finally {
      setBusy(false);
    }
  };

  const handleAssign = async (stage: IStageResponse, teamIds: GUID[]) => {
    const teams = teamIds
      .map(id => enrolledTeams.find(team => team.id === id))
      .filter((team): team is ITeamResponse => team !== undefined);

    setBusy(true);
    try {
      const ok = await assignTeamsToStage(stage.id, teamIds);
      if (ok) {
        addTeamsToGroupLocally(stage, teams);
        setPicker(null);
        await refreshCompletability();
      }
    } finally {
      setBusy(false);
    }
  };

  const handleRemoveFromGroup = async (
    stage: IStageResponse,
    team: ITeamResponse
  ) => {
    setBusy(true);
    try {
      const ok = await unassignTeamsFromStage(stage.id, [team.id]);
      if (ok) {
        removeTeamFromGroupLocally(stage, team.id);
        await refreshCompletability();
      }
    } finally {
      setBusy(false);
    }
  };

  // HU-122: a manual move between two sub-groups is unrestricted above the
  // backend's minimum-4-per-sub-group floor — the frontend adds no extra
  // guardrail here beyond that, letting the backend's own 409
  // (SubGroupReassignmentBelowMinimum) surface via the standard error toast
  // when the source sub-group would drop too low.
  const handleReassign = async (toStageId: GUID) => {
    if (!reassignMenu) return;
    const { division, fromStage, team } = reassignMenu;
    setReassignMenu(null);

    setBusy(true);
    try {
      const ok = await reassignTeamToSubGroup(
        division.id,
        team.id,
        fromStage.id,
        toStageId
      );
      if (ok) {
        await reloadDivision(division);
        await refreshCompletability();
      }
    } finally {
      setBusy(false);
    }
  };

  const handleAutoDistribute = async (division: IDivisionResponse) => {
    setBusy(true);
    try {
      const ok = await autoDistribute(division.id);
      if (ok) {
        await reloadDivision(division);
        await refreshCompletability();
      }
    } finally {
      setBusy(false);
    }
  };

  const handleRebuildSubGroups = async (subGroupCount: number) => {
    if (!rebuildTarget) return;
    const division = rebuildTarget;

    setBusy(true);
    try {
      const ok = await rebuildSubGroups(division.id, subGroupCount);
      if (ok) {
        setRebuildTarget(null);
        await reloadDivision(division);
        await refreshCompletability();
      }
    } finally {
      setBusy(false);
    }
  };

  const handlePickerConfirm = (teamIds: GUID[]) => {
    if (!picker) return;
    if (picker.kind === 'roster') {
      void handleEnroll(picker.division, teamIds);
    } else {
      void handleAssign(picker.stage, teamIds);
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

    // Block the whole screen while starting (closing registration + generating
    // the fixture can take a moment) and hard-reload on success so every view
    // reflects the started tournament.
    setBusy(true);
    const blockingMessageId = setBlockingMessage(
      'Iniciando el torneo y generando el fixture. No cierres ni cambies de pantalla…'
    );
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

      window.location.reload();
    } finally {
      clearBlockingMessage(blockingMessageId);
      setBusy(false);
    }
  };

  // Safety net over the backend completability: any tournament-registered
  // team not yet on ANY division's roster. A team must be enrolled in a
  // division's roster before it can be placed in a sub-group or drawn into a
  // bracket, so this is the roster-era equivalent of the old "no zone yet"
  // pool.
  const rosterTeamIds = useMemo(() => {
    const set = new Set<GUID>();
    assignments.forEach(({ roster }) =>
      roster.forEach(team => set.add(team.id))
    );
    return set;
  }, [assignments]);

  const unassignedTeams = useMemo(
    () => enrolledTeams.filter(team => !rosterTeamIds.has(team.id)),
    [enrolledTeams, rosterTeamIds]
  );

  // Teams eligible for a sub-group: the division's roster minus any team
  // already placed in ANY of the division's sub-groups (never the reverse —
  // placement is a subset of enrollment). The old client-side cross-zone
  // exclusion is gone: the roster enrol endpoint enforces the one-regular-
  // division-plus-optional-cross-cup rule server-side and answers 409.
  const eligibleTeamsForStage = (
    assignment: DivisionAssignment,
    stageId: GUID
  ): ITeamResponse[] => {
    const placed = placedInAnyGroup(assignment.groups);
    const here = new Set(
      (assignment.groups.find(g => g.stage.id === stageId)?.assignedTeams ?? []).map(
        t => t.id
      )
    );
    return assignment.roster.filter(team => !placed.has(team.id) || here.has(team.id))
      .filter(team => !here.has(team.id));
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
  // without a division roster — a safety net over the backend completability
  // so the tournament can never start with unassigned teams / empty
  // divisions. Ready when every enrolled team has a division and the backend
  // agrees. Starting from an open-registration draft is allowed — handleStart
  // closes the registration first before starting, so no separate step is
  // needed.
  const readyToStart = (completability?.canStart ?? false) && !hasUnassigned;

  return (
    <Box sx={{ width: '100%' }}>
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={1}
        sx={{ alignItems: { sm: 'center' }, justifyContent: 'space-between', mb: 2 }}
      >
        <Typography variant="h6">Asignación de equipos a divisiones</Typography>
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
          Podés ir inscribiendo equipos en cada división como borrador mientras la
          inscripción sigue abierta. Cuando estén todos ubicados, «Iniciar
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
                  primary={`Hay ${unassignedTeams.length} equipo(s) sin ninguna división asignada. Inscribílos todos antes de iniciar.`}
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

      {/* Pool of tournament-registered teams not yet on ANY division's roster. */}
      <Box
        component="section"
        aria-label="Equipos sin división"
        sx={{
          border: 1,
          borderColor: unassignedTeams.length > 0 ? 'warning.main' : 'divider',
          borderRadius: 1,
          p: 2,
          mb: 3,
        }}
      >
        <Typography variant="subtitle2" sx={{ mb: 1 }}>
          Equipos sin división ({unassignedTeams.length})
        </Typography>
        {unassignedTeams.length === 0 ? (
          <Typography variant="body2" sx={{ color: 'text.secondary' }}>
            Todos los equipos inscriptos están en alguna división.
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
            const { division, groups, roster, firstRoundStage } = assignment;
            // Headings distinguish sub-groups whenever a division has more
            // than one (a multi-sub-group regular zone, HU-121, or a
            // cross-division cup's parallel groups, HU-110) — a single-group
            // division shows a plain "Equipos" heading either way.
            const showGroupHeadings = groups.length > 1;
            const isCollapsed = collapsed.has(division.id);

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
                    label={`${roster.length} equipos`}
                  />
                </Stack>

                <Collapse in={!isCollapsed}>
                  <Box sx={{ pt: 1.5 }}>
                    {/* Division roster panel — ALWAYS renders. This is the fix
                        for the playoffs-only dead-fallback bug: enrollment no
                        longer depends on a group stage existing. */}
                    <Box sx={{ mb: 2.5 }}>
                      <Stack
                        direction="row"
                        sx={{
                          alignItems: 'center',
                          justifyContent: 'space-between',
                          mb: 1,
                        }}
                      >
                        <Typography variant="subtitle2" sx={{ color: 'text.secondary' }}>
                          Equipos inscriptos en la división ({roster.length})
                        </Typography>
                        <Button
                          size="small"
                          variant="outlined"
                          startIcon={<AddIcon />}
                          disabled={busy}
                          onClick={() =>
                            setPicker({
                              kind: 'roster',
                              division,
                              title: `Inscribir en ${division.name}`,
                              eligible: enrolledTeams.filter(
                                team => !roster.some(r => r.id === team.id)
                              ),
                            })
                          }
                        >
                          Inscribir equipos
                        </Button>
                      </Stack>

                      {roster.length === 0 ? (
                        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                          Todavía no hay equipos inscriptos en esta división.
                        </Typography>
                      ) : (
                        <Stack spacing={0.5}>
                          {roster.map(team => (
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
                                aria-label={`Quitar ${team.name} de la división`}
                                disabled={busy}
                                onClick={() => void handleUnenroll(assignment, team)}
                              >
                                <CloseIcon fontSize="small" />
                              </IconButton>
                            </Stack>
                          ))}
                        </Stack>
                      )}
                    </Box>

                    <Divider sx={{ mb: 2 }} />

                    {groups.length > 0 ? (
                      <>
                        <Stack
                          direction="row"
                          spacing={0.5}
                          sx={{ alignItems: 'center', mb: 1.5, flexWrap: 'wrap' }}
                        >
                          <Button
                            size="small"
                            variant="outlined"
                            startIcon={<AutorenewIcon />}
                            disabled={busy}
                            onClick={() => void handleAutoDistribute(division)}
                          >
                            Auto-repartir
                          </Button>
                          <FieldInfoTooltip title="Vacía los sub-grupos actuales y reparte de nuevo todo el roster de la división, siempre balanceado (nunca una diferencia de 2 o más equipos entre el sub-grupo más chico y el más grande, mínimo 4 por sub-grupo)." />
                          <Button
                            size="small"
                            variant="text"
                            startIcon={<TuneIcon />}
                            disabled={busy}
                            onClick={() => setRebuildTarget(division)}
                          >
                            Editar cantidad de sub-grupos
                          </Button>
                        </Stack>

                        <Stack spacing={2}>
                          {groups.map(({ stage, assignedTeams }) => (
                            <Box key={stage.id} component="section" aria-label={stage.name}>
                              <Stack
                                direction="row"
                                sx={{
                                  alignItems: 'center',
                                  justifyContent: 'space-between',
                                  mb: 1,
                                }}
                              >
                                <Typography
                                  variant="subtitle2"
                                  sx={{ color: 'text.secondary' }}
                                >
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
                                      kind: 'stage',
                                      stage,
                                      title: showGroupHeadings
                                        ? `${division.name} · ${stage.name}`
                                        : division.name,
                                      eligible: eligibleTeamsForStage(assignment, stage.id),
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
                                  Todavía no hay equipos en este sub-grupo.
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
                                      <Stack direction="row">
                                        {groups.length > 1 && (
                                          <IconButton
                                            size="small"
                                            aria-label={`Mover ${team.name} a otro sub-grupo`}
                                            disabled={busy}
                                            onClick={e =>
                                              setReassignMenu({
                                                anchorEl: e.currentTarget,
                                                division,
                                                fromStage: stage,
                                                team,
                                              })
                                            }
                                          >
                                            <SwapHorizIcon fontSize="small" />
                                          </IconButton>
                                        )}
                                        <IconButton
                                          size="small"
                                          aria-label={`Quitar ${team.name}`}
                                          disabled={busy}
                                          onClick={() =>
                                            void handleRemoveFromGroup(stage, team)
                                          }
                                        >
                                          <CloseIcon fontSize="small" />
                                        </IconButton>
                                      </Stack>
                                    </Stack>
                                  ))}
                                </Stack>
                              )}
                            </Box>
                          ))}
                        </Stack>
                      </>
                    ) : (
                      // Playoffs-only division (HU-128): no group stage, so the
                      // sub-group layer is replaced by the draw trigger.
                      <Box>
                        <Stack
                          direction="row"
                          spacing={0.5}
                          sx={{ alignItems: 'center', mb: 1, flexWrap: 'wrap' }}
                        >
                          <Button
                            size="small"
                            variant="outlined"
                            startIcon={<CasinoIcon />}
                            disabled={busy || !firstRoundStage || roster.length < 2}
                            onClick={() => setDrawTarget(assignment)}
                          >
                            {firstRoundStage?.drawnAt ? 'Volver a sortear' : 'Sortear llave'}
                          </Button>
                          <Button
                            size="small"
                            variant="text"
                            startIcon={<TuneIcon />}
                            disabled={busy}
                            onClick={() => setRebuildTarget(division)}
                          >
                            Armar sub-grupos
                          </Button>
                        </Stack>
                        {firstRoundStage?.drawnAt && (
                          <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                            Sorteo realizado el {formatDateAr(firstRoundStage.drawnAt)}
                          </Typography>
                        )}
                        {!firstRoundStage && (
                          <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                            Esta división todavía no tiene una llave de playoffs
                            configurada.
                          </Typography>
                        )}
                      </Box>
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
        onConfirm={handlePickerConfirm}
      />

      <RebuildSubGroupsDialog
        division={rebuildTarget}
        currentCount={
          rebuildTarget
            ? assignments.find(a => a.division.id === rebuildTarget.id)?.groups.length || 1
            : 1
        }
        busy={busy}
        onClose={() => setRebuildTarget(null)}
        onConfirm={handleRebuildSubGroups}
      />

      <Menu
        anchorEl={reassignMenu?.anchorEl ?? null}
        open={reassignMenu !== null}
        onClose={() => setReassignMenu(null)}
      >
        {(assignments.find(a => a.division.id === reassignMenu?.division.id)?.groups ?? [])
          .filter(group => group.stage.id !== reassignMenu?.fromStage.id)
          .map(group => (
            <MenuItem key={group.stage.id} onClick={() => void handleReassign(group.stage.id)}>
              {group.stage.name}
            </MenuItem>
          ))}
      </Menu>

      {drawTarget && drawTarget.firstRoundStage && (
        <PlayoffDrawDialog
          open
          onClose={() => setDrawTarget(null)}
          stageId={drawTarget.firstRoundStage.id}
          roster={drawTarget.roster}
          onCommitted={() => {
            void reloadDivision(drawTarget.division);
            void refreshCompletability();
          }}
        />
      )}
    </Box>
  );
};

export default TournamentDivisionAssignment;
