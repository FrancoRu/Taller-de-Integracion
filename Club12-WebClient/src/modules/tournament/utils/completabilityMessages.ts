import { ICompletabilityIssue } from '@/modules/tournament/type/tournament';

/**
 * Builds a neutral, professional Spanish message describing a single
 * completability issue (HU-109). The backend only sends a stable `code` plus
 * the fields relevant to that code; the copy lives entirely on the client so
 * the wording can evolve without a backend change. Unknown codes degrade to a
 * generic message rather than surfacing a raw code to the admin.
 *
 * @param {ICompletabilityIssue} issue - The issue to describe.
 * @returns {string} The human-readable Spanish message.
 */
export const completabilityIssueMessage = (
  issue: ICompletabilityIssue
): string => {
  switch (issue.code) {
    case 'ZoneTooFewTeams':
      return `La zona ${issue.divisionName} tiene ${issue.assignedTeams} equipos (mínimo 2).`;
    case 'TeamNotAssigned':
      return `${issue.teamName} está inscripto pero sin zona asignada.`;
    case 'TeamInMultipleZones':
      return `${issue.teamName} está asignado a más de una zona.`;
    case 'PlayoffRangeExceedsTeams':
      return `En ${issue.divisionName}, un rango de playoff arranca en la posición ${issue.fromPosition} pero solo hay ${issue.assignedTeams} equipos.`;
    case 'CrossCupGroupTooFewTeams':
      return `Un grupo de la copa cruzada tiene ${issue.assignedTeams} equipos (mínimo 2).`;
    case 'TeamTooFewPlayers':
      return `${issue.teamName} tiene ${issue.playerCount} jugador(es) (mínimo 5).`;
    default:
      return 'Hay un problema de configuración que impide iniciar el torneo.';
  }
};
