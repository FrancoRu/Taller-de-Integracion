import { GUID } from '@/modules/core/types/types';
import { CupConfig, STAGE_TYPE_LABELS, WizardState } from './types';

/**
 * A single step's validation result: empty when valid, otherwise the list
 * of problems to show the admin (first one is surfaced in the UI).
 */
export type ValidationResult = string[];

export const validateTournamentStep = (state: WizardState): ValidationResult => {
  const { tournament } = state;
  const errors: string[] = [];

  if (!tournament.name.trim()) {
    errors.push('El nombre del torneo es obligatorio.');
  }

  if (!tournament.startDate) {
    errors.push('La fecha de inicio es obligatoria.');
  }

  if (!tournament.teamRegistrationDeadline) {
    errors.push('La fecha límite de inscripción es obligatoria.');
  }

  if (
    tournament.startDate &&
    tournament.teamRegistrationDeadline &&
    new Date(tournament.teamRegistrationDeadline) >= new Date(tournament.startDate)
  ) {
    errors.push('La fecha límite de inscripción debe ser anterior a la fecha de inicio.');
  }

  return errors;
};

export const validateTeamsStep = (state: WizardState): ValidationResult => {
  const { selectedTeamIds } = state;
  const errors: string[] = [];

  if (selectedTeamIds.length < 1) {
    errors.push('Debés inscribir al menos un equipo.');
  }

  return errors;
};

const validateCups = (cups: CupConfig[], contextLabel: string): ValidationResult => {
  const errors: string[] = [];
  const seenNames = new Set<string>();

  cups.forEach(cup => {
    const trimmedName = cup.name.trim();

    if (!trimmedName) {
      errors.push(`Cada copa de ${contextLabel} necesita un nombre.`);
      return;
    }

    const normalized = trimmedName.toLowerCase();
    if (seenNames.has(normalized)) {
      errors.push(`Hay dos copas llamadas "${trimmedName}" en ${contextLabel}.`);
    }
    seenNames.add(normalized);

    if (cup.rounds.length === 0) {
      errors.push(`La copa "${trimmedName}" necesita al menos una ronda.`);
    }
  });

  return errors;
};

/**
 * Validates the zones step: every zone has a name, every selected team
 * belongs to exactly one zone (catches both "unassigned team" and "team
 * in two zones" before the admin ever reaches the server-side guardrails),
 * and every configured cup is well-formed.
 */
export const validateZonesStep = (state: WizardState): ValidationResult => {
  const errors: string[] = [];

  if (state.zones.length === 0) {
    errors.push('Agregá al menos una zona.');
    return errors;
  }

  const seenZoneNames = new Set<string>();
  state.zones.forEach(zone => {
    const trimmedName = zone.name.trim();
    if (!trimmedName) {
      errors.push('Todas las zonas necesitan un nombre.');
      return;
    }

    const normalized = trimmedName.toLowerCase();
    if (seenZoneNames.has(normalized)) {
      errors.push(`Hay dos zonas llamadas "${trimmedName}".`);
    }
    seenZoneNames.add(normalized);
  });

  const teamZoneCount = new Map<GUID, number>();
  for (const zone of state.zones) {
    for (const teamId of zone.teamIds) {
      teamZoneCount.set(teamId, (teamZoneCount.get(teamId) ?? 0) + 1);
    }
  }

  const unassigned = state.selectedTeamIds.filter(id => !teamZoneCount.has(id));
  if (unassigned.length > 0) {
    errors.push(
      `${unassigned.length} equipo(s) inscripto(s) todavía no están en ninguna zona.`
    );
  }

  const inMultipleZones = [...teamZoneCount.entries()].filter(([, count]) => count > 1);
  if (inMultipleZones.length > 0) {
    errors.push(`${inMultipleZones.length} equipo(s) están en más de una zona a la vez.`);
  }

  state.zones.forEach(zone => {
    if (zone.teamIds.length === 0) {
      errors.push(`La zona "${zone.name || '(sin nombre)'}" no tiene equipos.`);
    }

    errors.push(...validateCups(zone.cups, `la zona "${zone.name || '(sin nombre)'}"`));
  });

  return errors;
};

export const validateCrossCupStep = (state: WizardState): ValidationResult => {
  const { crossCup } = state;
  const errors: string[] = [];

  if (!crossCup.enabled) {
    return errors;
  }

  if (!crossCup.name.trim()) {
    errors.push('La copa cruzada necesita un nombre.');
  }

  const teamCount = crossCup.includeAllTeams
    ? state.selectedTeamIds.length
    : crossCup.teamIds.length;

  if (teamCount < 2) {
    errors.push('La copa cruzada necesita al menos 2 equipos.');
  }

  errors.push(...validateCups(crossCup.cups, 'la copa cruzada'));

  return errors;
};

export const isWizardReadyToSubmit = (state: WizardState): boolean =>
  validateTournamentStep(state).length === 0 &&
  validateTeamsStep(state).length === 0 &&
  validateZonesStep(state).length === 0 &&
  validateCrossCupStep(state).length === 0;

/** A single line of the review step's tree preview. */
export interface WizardTreeNode {
  id: string;
  depth: 1 | 2 | 3;
  label: string;
  tag?: string;
}

const describeCup = (cup: CupConfig): string => {
  const roundsSummary = cup.rounds
    .map(round => `${STAGE_TYPE_LABELS[round.stageType]} Bo${round.bestOf}`)
    .join(', ');
  return `${cup.name || '(sin nombre)'}${roundsSummary ? ` (${roundsSummary})` : ''}`;
};

const buildGroupAndCupNodes = (
  parentId: string,
  hasGroupStage: boolean,
  roundRobinLegs: number,
  cups: CupConfig[]
): WizardTreeNode[] => {
  const nodes: WizardTreeNode[] = [];

  if (hasGroupStage) {
    nodes.push({
      id: `${parentId}-group`,
      depth: 3,
      label:
        roundRobinLegs > 1
          ? `Fase de grupos (todos contra todos, ${roundRobinLegs} veces)`
          : 'Fase de grupos (todos contra todos)',
    });
  }

  cups.forEach(cup => {
    nodes.push({ id: cup.id, depth: 3, label: describeCup(cup) });
  });

  return nodes;
};

/**
 * Builds the flat tree preview shown in the review step — one line per
 * tournament / zone / (group stage or cup), mirroring the wireframe.
 */
export const buildWizardTree = (state: WizardState): WizardTreeNode[] => {
  const nodes: WizardTreeNode[] = [
    {
      id: 'tournament',
      depth: 1,
      label: state.tournament.name || '(sin nombre)',
      tag: `${state.selectedTeamIds.length} equipos`,
    },
  ];

  state.zones.forEach(zone => {
    nodes.push({
      id: zone.id,
      depth: 2,
      label: `${zone.name || '(sin nombre)'} — ${zone.teamIds.length} equipos`,
    });
    nodes.push(...buildGroupAndCupNodes(zone.id, zone.hasGroupStage, zone.roundRobinLegs, zone.cups));
  });

  if (state.crossCup.enabled) {
    const teamCount = state.crossCup.includeAllTeams
      ? state.selectedTeamIds.length
      : state.crossCup.teamIds.length;

    nodes.push({
      id: 'cross-cup',
      depth: 2,
      label: `${state.crossCup.name || '(sin nombre)'} — ${teamCount} equipos`,
      tag: 'división cruzada',
    });
    nodes.push(
      ...buildGroupAndCupNodes(
        'cross-cup',
        state.crossCup.hasGroupStage,
        state.crossCup.roundRobinLegs,
        state.crossCup.cups
      )
    );
  }

  return nodes;
};

export const resolveCrossCupTeamIds = (state: WizardState): GUID[] =>
  state.crossCup.includeAllTeams ? state.selectedTeamIds : state.crossCup.teamIds;
