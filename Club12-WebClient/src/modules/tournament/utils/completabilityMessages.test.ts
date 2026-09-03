import { describe, expect, it } from 'vitest';
import { completabilityIssueMessage } from '@/modules/tournament/utils/completabilityMessages';
import type { ICompletabilityIssue } from '@/modules/tournament/type/tournament.d';

describe('completabilityIssueMessage', () => {
  it('describes a zone with too few teams', () => {
    const issue: ICompletabilityIssue = {
      code: 'ZoneTooFewTeams',
      divisionName: 'Zona A',
      assignedTeams: 1,
    };

    expect(completabilityIssueMessage(issue)).toBe(
      'La zona Zona A tiene 1 equipos (mínimo 2).'
    );
  });

  it('describes an enrolled team without a zone', () => {
    const issue: ICompletabilityIssue = {
      code: 'TeamNotAssigned',
      teamName: 'River',
    };

    expect(completabilityIssueMessage(issue)).toBe(
      'River está inscripto pero sin zona asignada.'
    );
  });

  it('describes a team assigned to more than one zone', () => {
    const issue: ICompletabilityIssue = {
      code: 'TeamInMultipleZones',
      teamName: 'Boca',
    };

    expect(completabilityIssueMessage(issue)).toBe(
      'Boca está asignado a más de una zona.'
    );
  });

  it('describes a playoff range that exceeds the assigned teams', () => {
    const issue: ICompletabilityIssue = {
      code: 'PlayoffRangeExceedsTeams',
      divisionName: 'Zona B',
      fromPosition: 5,
      assignedTeams: 3,
    };

    expect(completabilityIssueMessage(issue)).toBe(
      'En Zona B, un rango de playoff arranca en la posición 5 pero solo hay 3 equipos.'
    );
  });

  it('describes a cross-cup group with too few teams', () => {
    const issue: ICompletabilityIssue = {
      code: 'CrossCupGroupTooFewTeams',
      assignedTeams: 1,
    };

    expect(completabilityIssueMessage(issue)).toBe(
      'Un grupo de la copa cruzada tiene 1 equipos (mínimo 2).'
    );
  });

  it('describes a team with too few habilitado players', () => {
    const issue: ICompletabilityIssue = {
      code: 'TeamTooFewPlayers',
      teamName: 'Independiente',
      playerCount: 3,
    };

    expect(completabilityIssueMessage(issue)).toBe(
      'Independiente tiene 3 jugador(es) habilitado(s) (mínimo 4).'
    );
  });

  it('falls back to a generic message for an unknown code', () => {
    const issue: ICompletabilityIssue = { code: 'SomethingElse' };

    expect(completabilityIssueMessage(issue)).toBe(
      'Hay un problema de configuración que impide iniciar el torneo.'
    );
  });
});
