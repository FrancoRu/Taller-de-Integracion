import { IMatchResponse } from '@/modules/match/type/match.d';
import { IStageResponse, StageType } from '@/modules/stage/type/stage';
import { stageLabel } from '@/modules/stage/utils/stageLabel';
import { translateStageType } from '@/modules/core/utils/translateStageType';

const STAGE_NAME_DIVISION_SEPARATOR = ' - ';

/**
 * Canonical chronological order of stage types, used ahead of a stage's own `order` field —
 * ThirdPlace and Final in particular are both leaf rounds with no reliable relative `order`
 * between them, so the phase itself is the only signal that sorts them correctly.
 */
const STAGE_TYPE_ORDER: Partial<Record<StageType, number>> = {
  [StageType.Group]: 0,
  [StageType.RoundOf16]: 1,
  [StageType.QuarterFinal]: 2,
  [StageType.SemiFinal]: 3,
  [StageType.ThirdPlace]: 4,
  [StageType.Final]: 5,
};

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
 * Groups a division's matches into ordered, labelled fixture sections — one per stage that has
 * at least one match. Stages are ordered by their phase (Group -> ... -> SemiFinal -> ThirdPlace
 * -> Final), ties broken by their own `order` and then a natural-numeric name compare, and empty
 * sections are dropped.
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
  const stagesInOrder = [...stages].sort((a, b) => {
    const typeOrderA = STAGE_TYPE_ORDER[a.stageType] ?? Number.MAX_SAFE_INTEGER;
    const typeOrderB = STAGE_TYPE_ORDER[b.stageType] ?? Number.MAX_SAFE_INTEGER;
    return (
      typeOrderA - typeOrderB ||
      a.order - b.order ||
      a.name.localeCompare(b.name, 'es', { numeric: true })
    );
  });
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

/** One cup's (or the default bracket's) fixture sections, grouped together. */
export interface BracketFixtureGroup {
  /** Null for a division with a single, unnamed bracket (no sub-cups). */
  bracketName: string | null;
  sections: DivisionFixtureSection[];
}

/**
 * Groups a playoff division's fixture sections by their cup (Copa Oro, Copa
 * Plata, …) so each cup renders as its own clearly separated block instead
 * of every round from every cup reading as one flat, undifferentiated list —
 * a division with two cups round-by-round otherwise interleaves as
 * "Semifinal (Oro)", "Semifinal (Plata)", "Final (Oro)", "Final (Plata)"
 * with nothing visually tying each cup's own rounds together.
 *
 * Once a cup has its own group header, repeating its name on every one of
 * its rounds is noise (the same lesson as not repeating "Fase final" on
 * every knockout round) — each section's label is stripped down to just the
 * round name for a named bracket, keeping the full "{bracket} — {round}"
 * label only for the unnamed default bracket, which has no group header of
 * its own to carry that context instead.
 */
export const groupFixtureSectionsByBracket = (
  sections: DivisionFixtureSection[]
): BracketFixtureGroup[] => {
  const groups: BracketFixtureGroup[] = [];
  const groupIndexByBracket = new Map<string | null, number>();

  sections.forEach(section => {
    const bracketName = section.stage.bracketName ?? null;
    const label = bracketName ? translateStageType(section.stage.stageType) : section.label;
    const relabelled: DivisionFixtureSection = { ...section, label };

    const existingIndex = groupIndexByBracket.get(bracketName);
    if (existingIndex !== undefined) {
      groups[existingIndex].sections.push(relabelled);
      return;
    }

    groupIndexByBracket.set(bracketName, groups.length);
    groups.push({ bracketName, sections: [relabelled] });
  });

  return groups;
};
