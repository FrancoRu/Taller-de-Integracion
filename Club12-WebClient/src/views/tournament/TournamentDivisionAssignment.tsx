import React, { useEffect, useState } from 'react';
import {
  Alert,
  AlertTitle,
  Box,
  Button,
  Chip,
  Divider,
  List,
  ListItem,
  ListItemText,
  Stack,
  Typography,
} from '@mui/material';
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
  IPutTournamentRequest,
} from '@/modules/tournament/type/tournament';
import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';
import { FILTER_OPTIONS_PAGE_SIZE } from '@/modules/core/constants/pagination';
import { confirmAction } from '@/modules/core/utils/confirmDialog';
import { completabilityIssueMessage } from '@/modules/tournament/utils/completabilityMessages';
import { DetailSkeleton } from '@/views/core/components/skeletons';

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

/**
 * Division-assignment workspace for the RegistrationClosed phase (HU-108 +
 * HU-109). For every regular zone it lists the teams assigned to that zone's
 * group stage and offers the enrolled-but-unassigned teams to add — enforcing
 * "one team, one zone" across every regular zone while leaving the
 * cross-division cup as a parallel, independent membership. A live
 * completability panel surfaces every blocking issue and gates the "Iniciar
 * torneo" transition until the backend reports the tournament can start.
 */
const TournamentDivisionAssignment: React.FC<
  TournamentDivisionAssignmentProps
> = ({ tournament }) => {
  const { getTeamsByFiltered } = useTeam();
  const { getStagesByFilters, assignTeamsToStage } = useStage();
  const { getDivisionsByFilters } = useDivision();
  const { getCompletability, putTournamentById } = useTournament();

  const [loading, setLoading] = useState(false);
  const [assignments, setAssignments] = useState<DivisionAssignment[]>([]);
  const [enrolledTeams, setEnrolledTeams] = useState<ITeamResponse[]>([]);
  const [completability, setCompletability] =
    useState<ITournamentCompletability | null>(null);
  const [busy, setBusy] = useState(false);
  const [reloadToken, setReloadToken] = useState(0);

  const isRegistrationClosed =
    tournament.status === TournamentStatus.RegistrationClosed;

  useEffect(() => {
    if (!isRegistrationClosed) {
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

            // A cross-division cup (HU-110) has more than one Group stage, so
            // we keep every one and let the admin assign teams to each; a
            // regular zone still resolves to a single group. Order them by
            // their stage `order` so "Grupo 1"…"Grupo N" stay in sequence.
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
    isRegistrationClosed,
    tournament.id,
    reloadToken,
    getCompletability,
    getDivisionsByFilters,
    getTeamsByFiltered,
    getStagesByFilters,
  ]);

  if (!isRegistrationClosed) {
    return (
      <Typography variant="body2" sx={{ color: 'text.secondary' }}>
        La asignación estará disponible cuando la inscripción del torneo esté
        cerrada.
      </Typography>
    );
  }

  if (loading) {
    return <DetailSkeleton />;
  }

  // "One team, one zone": a team assigned to any regular zone must not be
  // offered in another. The cross-division cup is a parallel membership, so it
  // is excluded from this set and only filters against its own assignments.
  const teamsInRegularZones = new Set<GUID>();
  assignments.forEach(({ division, groups }) => {
    if (!division.isCrossDivisionCup) {
      groups.forEach(group =>
        group.assignedTeams.forEach(team => teamsInRegularZones.add(team.id))
      );
    }
  });

  // Teams eligible for a division's groups. A regular zone bars any team
  // already placed in another regular zone. A cross-division cup (HU-110) is a
  // parallel membership: a team may join a cup group even if it also plays a
  // regular zone, but it must not sit in two different groups of the SAME cup,
  // so every team already in ANY of this cup's groups is excluded from all of
  // them.
  const eligibleTeamsFor = ({
    division,
    groups,
  }: DivisionAssignment): ITeamResponse[] => {
    if (division.isCrossDivisionCup) {
      const alreadyInCup = new Set<GUID>();
      groups.forEach(group =>
        group.assignedTeams.forEach(team => alreadyInCup.add(team.id))
      );
      return enrolledTeams.filter(team => !alreadyInCup.has(team.id));
    }

    return enrolledTeams.filter(team => !teamsInRegularZones.has(team.id));
  };

  const handleAssign = async (groupStageId: GUID, teamId: GUID) => {
    setBusy(true);
    try {
      const success = await assignTeamsToStage(groupStageId, [teamId]);
      if (success) {
        setReloadToken(token => token + 1);
      }
    } finally {
      setBusy(false);
    }
  };

  const handleStart = async () => {
    const confirmed = await confirmAction({
      title: 'Iniciar torneo',
      text: 'Se generará el fixture y comenzará el torneo. Esta acción no se puede revertir. ¿Continuar?',
      confirmButtonText: 'Iniciar torneo',
    });

    if (!confirmed) {
      return;
    }

    // Reuse the existing status-change flow: the tournament PUT routes the
    // requested status through the backend state machine, which rejects a
    // not-yet-completable start with 409 (surfaced by the global error
    // handler). Refetch completability afterwards so the panel reflects the
    // server's current verdict.
    const payload: IPutTournamentRequest = {
      name: tournament.name,
      description: tournament.description,
      startDate: new Date(tournament.startDate),
      teamRegistrationDeadline: new Date(tournament.teamRegistrationDeadline),
      status: TournamentStatus.Ongoing,
    };

    setBusy(true);
    try {
      await putTournamentById(tournament.id, payload);
      setReloadToken(token => token + 1);
    } finally {
      setBusy(false);
    }
  };

  const canStart = completability?.canStart ?? false;
  const issues = completability?.issues ?? [];

  return (
    <Box sx={{ width: '100%' }}>
      <Typography variant="h6" sx={{ mb: 2 }}>
        Asignación de equipos a zonas
      </Typography>

      <Box sx={{ mb: 3 }}>
        {issues.length > 0 ? (
          <Alert severity="warning">
            <AlertTitle>
              El torneo todavía no puede iniciarse
            </AlertTitle>
            <List dense disablePadding>
              {issues.map((issue, index) => (
                <ListItem key={`${issue.code}-${index}`} disableGutters>
                  <ListItemText primary={completabilityIssueMessage(issue)} />
                </ListItem>
              ))}
            </List>
          </Alert>
        ) : (
          <Alert severity="success">
            El torneo está listo para iniciarse.
          </Alert>
        )}

        <Stack direction="row" sx={{ mt: 2 }}>
          <Button
            variant="contained"
            onClick={() => void handleStart()}
            disabled={!canStart || busy}
          >
            Iniciar torneo
          </Button>
        </Stack>
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
            const eligibleTeams = eligibleTeamsFor(assignment);
            // A cross-division cup shows its N groups each under its own
            // heading; a regular zone has a single group and needs none.
            const showGroupHeadings = division.isCrossDivisionCup;

            return (
              <Box
                key={division.id}
                component="section"
                aria-label={division.name}
                sx={{
                  border: 1,
                  borderColor: 'divider',
                  borderRadius: 1,
                  p: 2,
                }}
              >
                <Stack
                  direction="row"
                  spacing={1}
                  sx={{ alignItems: 'center', mb: 1 }}
                >
                  <Typography variant="subtitle1">{division.name}</Typography>
                  {division.isCrossDivisionCup && (
                    <Chip size="small" color="secondary" label="Copa cruzada" />
                  )}
                </Stack>

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
                        {showGroupHeadings && (
                          <Typography variant="subtitle2" sx={{ mb: 0.5 }}>
                            {stage.name}
                          </Typography>
                        )}

                        <Typography
                          variant="subtitle2"
                          sx={{ color: 'text.secondary', mb: 0.5 }}
                        >
                          Equipos asignados
                        </Typography>
                        {assignedTeams.length === 0 ? (
                          <Typography
                            variant="body2"
                            sx={{ color: 'text.secondary', mb: 1 }}
                          >
                            Todavía no hay equipos asignados a esta zona.
                          </Typography>
                        ) : (
                          <List dense disablePadding sx={{ mb: 1 }}>
                            {assignedTeams.map(team => (
                              <ListItem key={team.id} disableGutters>
                                <ListItemText primary={team.name} />
                              </ListItem>
                            ))}
                          </List>
                        )}

                        <Typography
                          variant="subtitle2"
                          sx={{ color: 'text.secondary', mb: 0.5 }}
                        >
                          Agregar equipos
                        </Typography>
                        {eligibleTeams.length === 0 ? (
                          <Typography
                            variant="body2"
                            sx={{ color: 'text.secondary' }}
                          >
                            No hay equipos disponibles para agregar a esta zona.
                          </Typography>
                        ) : (
                          <Stack
                            direction="row"
                            spacing={1}
                            sx={{ flexWrap: 'wrap', gap: 1 }}
                          >
                            {eligibleTeams.map(team => (
                              <Button
                                key={team.id}
                                size="small"
                                variant="outlined"
                                disabled={busy}
                                onClick={() =>
                                  void handleAssign(stage.id, team.id)
                                }
                              >
                                Agregar {team.name}
                              </Button>
                            ))}
                          </Stack>
                        )}
                      </Box>
                    ))}
                  </Stack>
                )}
              </Box>
            );
          })}
        </Stack>
      )}
    </Box>
  );
};

export default TournamentDivisionAssignment;
