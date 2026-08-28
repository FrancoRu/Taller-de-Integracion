import { TOURNAMENT_CATEGORY_LABELS } from '@/modules/core/enum/tournament/tournamentCategory';
import { CupConfig, PlayoffMappingConfig, STAGE_TYPE_LABELS, WizardState } from './types';

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
 * Validates a division's playoff-range → cup mappings (HU-45): every row
 * must point at a configured cup, sit within `1..teamCount`, have
 * `from <= to`, and no two rows may overlap. `teamCount` of 0 (no teams
 * assigned yet) skips the upper-bound check so the admin can still draft
 * ranges before finishing team assignment. `cupNames` is the set of the
 * division's configured cup names the destination must belong to.
 */
export const validatePlayoffMappings = (
  mappings: PlayoffMappingConfig[],
  teamCount: number,
  cupNames: string[],
  contextLabel: string
): ValidationResult => {
  const errors: string[] = [];

  if (mappings.length === 0) {
    return errors;
  }

  const validCupNames = new Set(cupNames.map(name => name.trim().toLowerCase()).filter(Boolean));

  mappings.forEach(mapping => {
    const { fromPosition, toPosition, destination } = mapping;

    if (!Number.isInteger(fromPosition) || !Number.isInteger(toPosition) || fromPosition < 1 || toPosition < 1) {
      errors.push(`Los rangos de playoff de ${contextLabel} deben usar posiciones enteras desde 1.`);
      return;
    }

    if (fromPosition > toPosition) {
      errors.push(
        `En ${contextLabel}, el rango ${fromPosition}–${toPosition} está invertido (desde debe ser ≤ hasta).`
      );
      return;
    }

    if (teamCount > 0 && toPosition > teamCount) {
      errors.push(
        `En ${contextLabel}, el rango ${fromPosition}–${toPosition} supera los ${teamCount} equipos de la zona.`
      );
    }

    const trimmedDestination = destination.trim();
    if (!trimmedDestination) {
      errors.push(`Cada rango de playoff de ${contextLabel} necesita una copa de destino.`);
    } else if (!validCupNames.has(trimmedDestination.toLowerCase())) {
      errors.push(
        `En ${contextLabel}, la copa de destino "${trimmedDestination}" no coincide con ninguna copa configurada.`
      );
    }
  });

  // Overlap check: sort by start position and confirm each range starts
  // strictly after the previous one ends, so no position lands in two cups.
  const sorted = [...mappings]
    .filter(m => Number.isInteger(m.fromPosition) && Number.isInteger(m.toPosition) && m.fromPosition <= m.toPosition)
    .sort((a, b) => a.fromPosition - b.fromPosition);

  for (let i = 1; i < sorted.length; i += 1) {
    if (sorted[i].fromPosition <= sorted[i - 1].toPosition) {
      errors.push(
        `Los rangos de playoff de ${contextLabel} se solapan (posición ${sorted[i].fromPosition} está en dos copas).`
      );
      break;
    }
  }

  return errors;
};

/**
 * Validates the zones step (HU-106 — STRUCTURE ONLY): every zone has a
 * unique name and every configured cup / playoff-range mapping is
 * well-formed. Teams are not selected in the wizard anymore, so there is no
 * team-partition check here; the playoff ranges are validated with a team
 * count of 0, which skips the upper-bound check while still catching
 * inverted, overlapping, or unmapped ranges.
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
    errors.push(
      ...validatePlayoffMappings(
        zone.playoffMappings,
        0,
        zone.cups.map(cup => cup.name),
        `la zona "${zone.name || '(sin nombre)'}"`
      )
    );
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

  errors.push(...validateCups(crossCup.cups, 'la copa cruzada'));
  errors.push(
    ...validatePlayoffMappings(
      crossCup.playoffMappings,
      0,
      crossCup.cups.map(cup => cup.name),
      'la copa cruzada'
    )
  );

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
      nodes.push({ id: cup.id, depth: 3, label: describeCup(cup) });
    });
  }

  return nodes;
};
