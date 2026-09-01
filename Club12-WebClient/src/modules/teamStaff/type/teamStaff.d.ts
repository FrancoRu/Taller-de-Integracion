import { GUID } from '@/modules/core/types/types';

/**
 * The role a technical staff member holds for a team's participation in a
 * tournament. Mirrors the backend `TeamStaffRole` enum.
 */
export type TeamStaffRole = 'Coach' | 'AssistantCoach' | 'PlayerCoach';

/**
 * A team's technical staff (cuerpo técnico) member, as returned by the create
 * and list endpoints. Mirrors the backend `TeamStaffResponse`. Scoped to a
 * single team+tournament participation (season-scoped).
 * @interface ITeamStaffResponse
 */
export interface ITeamStaffResponse {
  /** The unique identifier of the staff member. */
  id: GUID;

  /** The team this staff member belongs to. */
  teamId: GUID;

  /** The team's name, when the team was loaded. */
  teamName?: string;

  /** The tournament (season participation) this staff member is scoped to. */
  tournamentId: GUID;

  /** The staff member's full name. */
  fullName: string;

  /** The staff member's role (DT, Asistente, DT-Jugador). */
  role: TeamStaffRole;

  /** When the staff member was registered (ISO date). */
  dateCreated: string;
}

/**
 * The request body for adding a technical staff member to a team. The team is
 * taken from the route, not the body. Mirrors the backend
 * `CreateTeamStaffRequest`.
 * @interface ICreateTeamStaffRequest
 */
export interface ICreateTeamStaffRequest {
  /** The staff member's full name. */
  fullName: string;

  /** The staff member's role. */
  role: TeamStaffRole;

  /** The tournament (season participation) to scope this staff member to. */
  tournamentId: GUID;
}
