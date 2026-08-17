import { StageType } from '@/modules/stage/type/stage';

/**
 * Spanish translations for each StageType value.
 * Using `satisfies` enforces exhaustiveness at compile time.
 */
const STAGE_TYPE_ES = {
  [StageType.Group]: 'Fase de grupos',
  [StageType.RoundOf16]: 'Octavos de final',
  [StageType.QuarterFinal]: 'Cuartos de final',
  [StageType.SemiFinal]: 'Semifinal',
  [StageType.ThirdPlace]: 'Tercer puesto',
  [StageType.Final]: 'Final',
} satisfies Record<StageType, string>;

/**
 * Translates a StageType enum value into its Spanish equivalent.
 */
export const translateStageType = (stageType: StageType): string =>
  STAGE_TYPE_ES[stageType];
