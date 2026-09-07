import { describe, expect, it } from 'vitest';
import { StageType } from '@/modules/stage/type/stage';
import { CupConfig } from '@/views/tournament/wizard/types';
import { buildTemplateBracketModel } from './templateBracket';

const cup = (overrides: Partial<CupConfig> = {}): CupConfig => ({
  id: 'cup-1',
  name: 'Copa Oro',
  qualifiers: 4,
  bestOfByStage: {},
  hasThirdPlace: true,
  ...overrides,
});

describe('buildTemplateBracketModel', () => {
  it('builds a semifinal + final shape for 4 qualifiers, with a third-place side slot', () => {
    const model = buildTemplateBracketModel(cup());

    expect(model.rounds.map(round => round.stageType)).toEqual([
      StageType.SemiFinal,
      StageType.Final,
    ]);
    expect(model.rounds[0].matches).toHaveLength(2);
    expect(model.rounds[1].matches).toHaveLength(1);
    expect(model.thirdPlace).toBeDefined();
    expect(model.thirdPlace!.matches).toHaveLength(1);
    expect(model.edges).toEqual([]);
  });

  it('omits the third-place slot when the cup does not play one', () => {
    const model = buildTemplateBracketModel(cup({ hasThirdPlace: false }));

    expect(model.thirdPlace).toBeUndefined();
  });

  it('builds a full quarterfinal-through-final shape for 8 qualifiers', () => {
    const model = buildTemplateBracketModel(cup({ qualifiers: 8 }));

    expect(model.rounds.map(round => round.stageType)).toEqual([
      StageType.QuarterFinal,
      StageType.SemiFinal,
      StageType.Final,
    ]);
    expect(model.rounds[0].matches).toHaveLength(4);
    expect(model.rounds[1].matches).toHaveLength(2);
    expect(model.rounds[2].matches).toHaveLength(1);
  });

  it('builds a single final-only shape for 2 qualifiers', () => {
    const model = buildTemplateBracketModel(cup({ qualifiers: 2, hasThirdPlace: true }));

    expect(model.rounds.map(round => round.stageType)).toEqual([StageType.Final]);
    expect(model.rounds[0].matches).toHaveLength(1);
    // A 2-team cup has no semifinal losers to seed a third-place decider from.
    expect(model.thirdPlace).toBeUndefined();
  });

  it('every placeholder match has no participants and a unique id', () => {
    const model = buildTemplateBracketModel(cup({ qualifiers: 8 }));
    const allMatches = model.rounds.flatMap(round => round.matches);

    allMatches.forEach(match => {
      expect(match.homeTeam).toBeNull();
      expect(match.visitorTeam).toBeNull();
      expect(match.isFinished).toBe(false);
    });

    const ids = allMatches.map(match => match.id);
    expect(new Set(ids).size).toBe(ids.length);
  });

  it('scopes ids to the cup, so two cups never collide', () => {
    const modelA = buildTemplateBracketModel(cup({ id: 'cup-a' }));
    const modelB = buildTemplateBracketModel(cup({ id: 'cup-b' }));

    const idsA = new Set(modelA.rounds.flatMap(round => round.matches.map(match => match.id)));
    const idsB = modelB.rounds.flatMap(round => round.matches.map(match => match.id));

    idsB.forEach(id => expect(idsA.has(id)).toBe(false));
  });
});
