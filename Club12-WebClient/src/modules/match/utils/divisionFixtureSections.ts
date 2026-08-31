import { IMatchResponse } from '@/modules/match/type/match.d';
import { IStageResponse, StageType } from '@/modules/stage/type/stage';
import { stageLabel } from '@/modules/stage/utils/stageLabel';

const STAGE_NAME_DIVISION_SEPARATOR = ' - ';

/**
 * Stage names follow a "{Division} - {Specific}" convention (e.g.
 * "Copa Club12 - ZONA 3"). We're already inside that division's tab, so
 * strip the redundant prefix and show the specific part — this is what
 * distinguishes stages sharing the same type (e.g. a cup division with
 * several parallel group stages, all "Group" type with no bracketName,
 * which stageLabel() alone can't tell apart).
 */
export const stageSectionLabel = (stage: IStageResponse, divisionName: string): string => {
  const prefix = `${divisionName}${STAGE_NAME_DIVISION_SEPARATOR}`;
  return stage.name.startsWith(prefix) ? stage.name.slice(prefix.length) : stageLabel(stage);
};

export interface DivisionFixtureSection {
  stage: IStageResponse;
  label: string;
  matches: IMatchResponse[];
}

/**
 * Groups a division's matches into ordered, labelled fixture sections — one per
 * stage that has at least one match. Stages are ordered by their `order` (ties
 * broken by natural-numeric name compare), and empty sections are dropped.
 *
 * A multi-group cross-division cup has several parallel Group stages
 * ("Grupo 1".."Grupo N"). stageSectionLabel would collapse them all to the
 * generic "Fase de grupos", so each is labelled by its own stage name instead —
 * that is the only thing distinguishing one group's fixture from another.
 */
export const buildDivisionFixtureSections = (
  stages: IStageResponse[],
  matches: IMatchResponse[],
  divisionName: string
): DivisionFixtureSection[] => {
  const stagesInOrder = [...stages].sort(
    (a, b) => a.order - b.order || a.name.localeCompare(b.name, 'es', { numeric: true })
  );
  const groupStageCount = stages.filter(stage => stage.stageType === StageType.Group).length;
  return stagesInOrder
    .map(stage => {
      const isDistinctGroup = stage.stageType === StageType.Group && groupStageCount > 1;
      return {
        stage,
        label: isDistinctGroup ? stage.name : stageSectionLabel(stage, divisionName),
        matches: matches.filter(match => match.stageId === stage.id),
      };
    })
    .filter(section => section.matches.length > 0);
};
