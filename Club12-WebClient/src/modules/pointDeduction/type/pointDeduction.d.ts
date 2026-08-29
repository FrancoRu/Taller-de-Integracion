import { GUID } from '@/modules/core/types/types';

/**
 * A disciplinary point deduction (deducción de puntos) record, as returned by
 * the create and list endpoints. Mirrors the backend `PointDeductionResponse`.
 * @interface IPointDeductionResponse
 */
export interface IPointDeductionResponse {
  /** The unique identifier of the deduction. */
  id: GUID;

  /** The division whose standings the deduction affects. */
  divisionId: GUID;

  /** The penalised team's id. */
  teamId: GUID;

  /** The penalised team's name, when the team was loaded. */
  teamName?: string;

  /** The table points subtracted from the team's total. */
  points: number;

  /** The disciplinary reason (motivo) for the deduction. */
  reason: string;

  /** When the deduction was applied (ISO date). */
  dateCreated: string;
}

/**
 * The request body for applying a point deduction to a team in a division.
 * The division is taken from the route, not the body. Mirrors the backend
 * `CreatePointDeductionRequest`.
 * @interface IAddPointDeduction
 */
export interface IAddPointDeduction {
  /** The team to penalise. */
  teamId: GUID;

  /** The positive amount of table points to subtract (at least 1). */
  points: number;

  /** The disciplinary reason (motivo), up to 300 characters. */
  reason: string;
}
