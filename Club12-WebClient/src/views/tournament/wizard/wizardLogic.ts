import { TOURNAMENT_CATEGORY_LABELS } from '@/modules/core/enum/tournament/tournamentCategory';
import {
  CupConfig,
  MAX_CUP_QUALIFIERS,
  MIN_CUP_QUALIFIERS,
  STAGE_TYPE_LABELS,
  WizardState,
  getStageBestOf,
  qualifiersToStageTypes,
} from './types';

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

  // A tournament always belongs to a season — it is created within one.
  if (!tournament.seasonId) {
    errors.push('El torneo debe pertenecer a una temporada.');
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

/**
 * Validates a set of cups (HU-112): each needs a unique name and — for zone
 * cups — a whole "cuántos clasifican" between {@link MIN_CUP_QUALIFIERS} and
 * {@link MAX_CUP_QUALIFIERS}. The cross cup's qualifier count is derived from
 * its groups, so `checkQualifiers` is false there.
 */
const validateCups = (
  cups: CupConfig[],
  contextLabel: string,
  checkQualifiers = true
): ValidationResult => {
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

    if (
      checkQualifiers &&
      (!Number.isInteger(cup.qualifiers) ||
        cup.qualifiers < MIN_CUP_QUALIFIERS ||
        cup.qualifiers > MAX_CUP_QUALIFIERS)
    ) {
      errors.push(
        `La copa "${trimmedName}" debe tener entre ${MIN_CUP_QUALIFIERS} y ${MAX_CUP_QUALIFIERS} clasificados.`
      );
    }
  });

  return errors;
};

/**
 * Validates the zones step (HU-106 — STRUCTURE ONLY): every zone has a unique
 * name and every configured cup is well-formed (name + qualifier count). The
 * standings→cup position ranges are DERIVED from the cups (HU-112), so there
 * is no manual range editor to validate; the "ranges fit the teams" check runs
 * later, at assignment/start, as a completability rule (HU-109).
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

  state.zones.forEach(zone => {
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

  // HU-110: the cross cup is split into groups, and a fixed number of teams
  // advances from each group into the pooled bracket. Both must be whole
  // numbers of at least 1.
  if (!Number.isInteger(crossCup.groupCount) || crossCup.groupCount < 1) {
    errors.push('La copa cruzada necesita al menos un grupo.');
  }

  if (!Number.isInteger(crossCup.qualifiersPerGroup) || crossCup.qualifiersPerGroup < 1) {
    errors.push('Debe clasificar al menos un equipo por grupo en la copa cruzada.');
  }

  // The cross cup's cups derive their qualifiers from its groups, so only the
  // name/uniqueness is validated here.
  errors.push(...validateCups(crossCup.cups, 'la copa cruzada', false));

  return errors;
};

export const isWizardReadyToSubmit = (state: WizardState): boolean =>
  validateTournamentStep(state).length === 0 &&
  validateZonesStep(state).length === 0 &&
  validateCrossCupStep(state).length === 0;

/** A single line of the review step's tree preview. */
export interface WizardTreeNode {
  id: string;
  depth: 1 | 2 | 3;
  label: string;
  tag?: string;
}

/**
 * Describes a cup for the review tree (HU-112): its name, how many qualify,
 * the series format, and the derived bracket rounds. `qualifiers` defaults to
 * the cup's own count but the cross cup passes its pooled group total.
 */
const describeCup = (cup: CupConfig, qualifiers = cup.qualifiers): string => {
  const phases = qualifiersToStageTypes(qualifiers)
    .map(stageType => {
      const bestOf = getStageBestOf(cup, stageType);
      const serie = bestOf === 1 ? 'a partido único' : `al mejor de ${bestOf}`;
      return `${STAGE_TYPE_LABELS[stageType]} ${serie}`;
    })
    .join(' → ');
  return `${cup.name || '(sin nombre)'} — ${qualifiers} clasifican (${phases})`;
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
      label: `${state.tournament.name || '(sin nombre)'} · ${
        TOURNAMENT_CATEGORY_LABELS[state.tournament.category]
      }`,
    },
  ];

  state.zones.forEach(zone => {
    nodes.push({
      id: zone.id,
      depth: 2,
      label: zone.name || '(sin nombre)',
    });
    nodes.push(...buildGroupAndCupNodes(zone.id, zone.hasGroupStage, zone.roundRobinLegs, zone.cups));
  });

  if (state.crossCup.enabled) {
    const { groupCount, qualifiersPerGroup, roundRobinLegs, cups } = state.crossCup;
    const pooledQualifiers = groupCount * qualifiersPerGroup;

    nodes.push({
      id: 'cross-cup',
      depth: 2,
      label: state.crossCup.name || '(sin nombre)',
      tag: 'división cruzada',
    });

    // HU-110: one line per group, then a line stating how many advance.
    for (let groupNumber = 1; groupNumber <= groupCount; groupNumber += 1) {
      nodes.push({
        id: `cross-cup-group-${groupNumber}`,
        depth: 3,
        label:
          roundRobinLegs > 1
            ? `Grupo ${groupNumber} (todos contra todos, ${roundRobinLegs} veces)`
            : `Grupo ${groupNumber} (todos contra todos)`,
      });
    }

    nodes.push({
      id: 'cross-cup-qualifiers',
      depth: 3,
      label:
        qualifiersPerGroup === 1
          ? 'Clasifica 1 equipo por grupo'
          : `Clasifican ${qualifiersPerGroup} equipos por grupo`,
    });

    cups.forEach(cup => {
      nodes.push({ id: cup.id, depth: 3, label: describeCup(cup, pooledQualifiers) });
    });
  }

  return nodes;
};
