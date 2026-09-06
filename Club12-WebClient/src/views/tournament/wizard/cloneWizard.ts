import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';
import { StageType } from '@/modules/stage/type/stage';
import {
  IDivisionStructureResponse,
  IStageStructureResponse,
  ITournamentStructureResponse,
} from '@/modules/tournament/type/tournament.d';
import {
  CrossCupConfig,
  CupConfig,
  WizardState,
  ZoneConfig,
  createInitialWizardState,
  nextLocalId,
  qualifiersToStageTypes,
} from './types';

/**
 * The qualifier range a bracket's DEEPEST present elimination stage type
 * gates, mirroring {@link qualifiersToStageTypes}'s buckets in reverse. Used
 * only as the fallback when a cup's authoritative qualifier source (D1) is
 * missing or contradicts the actual persisted bracket shape.
 */
const RANGE_BY_DEEPEST_STAGE: [StageType, { min: number; max: number }][] = [
  [StageType.RoundOf16, { min: 9, max: 16 }],
  [StageType.QuarterFinal, { min: 5, max: 8 }],
  [StageType.SemiFinal, { min: 3, max: 4 }],
];

const rangeForStageTypes = (stageTypes: Set<StageType>): { min: number; max: number } => {
  const match = RANGE_BY_DEEPEST_STAGE.find(([deepest]) => stageTypes.has(deepest));
  return match ? match[1] : { min: 2, max: 2 };
};

const sameStageTypeSet = (a: Set<StageType>, b: Set<StageType>): boolean =>
  a.size === b.size && [...a].every(type => b.has(type));

/** One elimination bracket's stages, grouped by BracketName in first-appearance order. */
const groupElimStagesByBracket = (
  stages: IStageStructureResponse[]
): { bracketName: string; stages: IStageStructureResponse[] }[] => {
  const order: string[] = [];
  const byName = new Map<string, IStageStructureResponse[]>();

  stages.forEach(stage => {
    if (!stage.bracketName) {
      return;
    }
    if (!byName.has(stage.bracketName)) {
      order.push(stage.bracketName);
      byName.set(stage.bracketName, []);
    }
    byName.get(stage.bracketName)!.push(stage);
  });

  return order.map(bracketName => ({ bracketName, stages: byName.get(bracketName)! }));
};

/**
 * Builds one CupConfig from a bracket's stages (D1): `qualifiers` comes from
 * `authoritativeQualifiers` — the PlayoffMapping span for a regular cup, or
 * `groupCount * qualifiersPerGroup` for the cross cup — cross-checked against
 * the actual bracket shape by recomputing {@link qualifiersToStageTypes}. A
 * missing source or a mismatch NEVER guesses: it falls back to the actual
 * shape's minimum qualifier count and appends a review notice.
 */
const buildCupConfig = (
  bracketName: string,
  bracketStages: IStageStructureResponse[],
  authoritativeQualifiers: number | null,
  contextLabel: string,
  review: string[]
): CupConfig => {
  const hasThirdPlace = bracketStages.some(stage => stage.stageType === StageType.ThirdPlace);
  const actualTypes = new Set(bracketStages.map(stage => stage.stageType));
  const range = rangeForStageTypes(actualTypes);

  let qualifiers = authoritativeQualifiers ?? range.min;

  if (authoritativeQualifiers === null) {
    review.push(
      `La copa "${bracketName}" de ${contextLabel} no tiene un mapeo de clasificación asociado — se usó el mínimo (${range.min} clasificados) y debe revisarse.`
    );
  } else if (
    !sameStageTypeSet(new Set(qualifiersToStageTypes(authoritativeQualifiers, hasThirdPlace)), actualTypes)
  ) {
    qualifiers = range.min;
    review.push(
      `La copa "${bracketName}" de ${contextLabel} no coincide con la cantidad de clasificados esperada — se usó el mínimo (${range.min}) y debe revisarse.`
    );
  }

  const bestOfByStage: Partial<Record<StageType, number>> = {};
  bracketStages.forEach(stage => {
    bestOfByStage[stage.stageType] = stage.bestOf;
  });

  return {
    id: nextLocalId(),
    name: bracketName,
    qualifiers,
    bestOfByStage,
    hasThirdPlace,
  };
};

/**
 * Builds a regular (non-cross) division's cups (D1): grouped by BracketName,
 * `qualifiers` read from the matching PlayoffMapping span (`ToPosition -
 * FromPosition + 1`), ordered top-down by `FromPosition` — cups without a
 * matching mapping are appended after, in their original stage order.
 */
const buildRegularCups = (division: IDivisionStructureResponse, review: string[]): CupConfig[] => {
  const contextLabel = `la zona "${division.name || '(sin nombre)'}"`;
  const brackets = groupElimStagesByBracket(division.stages);

  const withMapping: { cup: CupConfig; fromPosition: number }[] = [];
  const withoutMapping: CupConfig[] = [];

  brackets.forEach(({ bracketName, stages }) => {
    const mapping = division.playoffMappings.find(m => m.destination === bracketName);
    const authoritativeQualifiers = mapping ? mapping.toPosition - mapping.fromPosition + 1 : null;
    const cup = buildCupConfig(bracketName, stages, authoritativeQualifiers, contextLabel, review);

    if (mapping) {
      withMapping.push({ cup, fromPosition: mapping.fromPosition });
    } else {
      withoutMapping.push(cup);
    }
  });

  withMapping.sort((a, b) => a.fromPosition - b.fromPosition);

  return [...withMapping.map(entry => entry.cup), ...withoutMapping];
};

/**
 * A division's non-cup Group-type stages' shared RoundRobinLegs value (D1).
 * Differing values across sub-groups are flagged and the minimum is used
 * instead of a silent guess.
 */
const resolveRoundRobinLegs = (
  groupStages: IStageStructureResponse[],
  mismatchMessage: (min: number) => string,
  review: string[]
): number => {
  const legsValues = [...new Set(groupStages.map(stage => stage.roundRobinLegs))];

  if (legsValues.length <= 1) {
    return legsValues[0] ?? 1;
  }

  const min = Math.min(...legsValues);
  review.push(mismatchMessage(min));
  return min;
};

/**
 * Builds one regular division's ZoneConfig (D1): `subGroupCount` from the
 * count of non-cup Group-type stages (0 means groupless/playoffs-only), and
 * cups from its elimination brackets.
 */
const buildZoneConfig = (division: IDivisionStructureResponse, review: string[]): ZoneConfig => {
  const groupStages = division.stages.filter(stage => stage.stageType === StageType.Group);
  const zoneName = division.name || '(sin nombre)';

  return {
    id: nextLocalId(),
    name: division.name,
    hasGroupStage: groupStages.length > 0,
    roundRobinLegs: resolveRoundRobinLegs(
      groupStages,
      min =>
        `La zona "${zoneName}" tiene sub-grupos con una cantidad de "todos contra todos" inconsistente — se usó el mínimo (${min}) y debe revisarse.`,
      review
    ),
    subGroupCount: Math.max(groupStages.length, 1),
    cups: buildRegularCups(division, review),
    pointsForWin: division.pointsForWin,
    pointsForLoss: division.pointsForLoss,
  };
};

/**
 * Builds the cross-division cup's CrossCupConfig (D1): `groupCount` from its
 * Group-type stages, `qualifiersPerGroup` read directly from the division,
 * and its pooled bracket derived the same way as a regular cup's, using
 * `groupCount * qualifiersPerGroup` as the (never-guessed) pooled total.
 */
const buildCrossCupConfig = (division: IDivisionStructureResponse, review: string[]): CrossCupConfig => {
  const groupStages = division.stages.filter(stage => stage.stageType === StageType.Group);
  const groupCount = Math.max(groupStages.length, 1);
  const cupName = division.name || '(sin nombre)';

  const roundRobinLegs = resolveRoundRobinLegs(
    groupStages,
    min =>
      `La copa cruzada "${cupName}" tiene grupos con una cantidad de "todos contra todos" inconsistente — se usó el mínimo (${min}) y debe revisarse.`,
    review
  );

  const pooledQualifiers = groupCount * division.qualifiersPerGroup;
  const contextLabel = `la copa cruzada "${cupName}"`;
  const cups = groupElimStagesByBracket(division.stages).map(({ bracketName, stages }) =>
    buildCupConfig(bracketName, stages, pooledQualifiers, contextLabel, review)
  );

  return {
    enabled: true,
    name: division.name,
    groupCount,
    qualifiersPerGroup: division.qualifiersPerGroup,
    roundRobinLegs,
    cups,
    pointsForWin: division.pointsForWin,
    pointsForLoss: division.pointsForLoss,
  };
};

/**
 * HU-cloning (D1): reverse-maps a source tournament's structure tree into a
 * WizardState pre-fill, cross-checking every derived numeric value against
 * the actual persisted shape and flagging — never silently guessing — on any
 * mismatch (`review[]`). Flagging one zone/cup never blocks the rest of the
 * structure from pre-filling correctly.
 *
 * Dates and season always come back blank — the wizard's existing validation
 * requires them before submit, unchanged. `category` is always the
 * organizer's explicit choice, threaded in here, and never inherited from the
 * source tournament.
 */
export const structureToWizardState = (
  dto: ITournamentStructureResponse,
  category: TournamentCategory
): { state: WizardState; review: string[] } => {
  const review: string[] = [];
  const initial = createInitialWizardState();

  const crossCupDivision = dto.divisions.find(division => division.isCrossDivisionCup);
  const zoneDivisions = dto.divisions.filter(division => !division.isCrossDivisionCup);

  const state: WizardState = {
    tournament: {
      ...initial.tournament,
      name: `${dto.name} (copia)`,
      description: dto.description ?? '',
      category,
    },
    zones: zoneDivisions.map(division => buildZoneConfig(division, review)),
    crossCup: crossCupDivision ? buildCrossCupConfig(crossCupDivision, review) : initial.crossCup,
  };

  return { state, review };
};
