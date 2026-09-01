import { GUID } from '@/modules/core/types/types';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';
import { StageType } from '@/modules/stage/type/stage';
import { PlayoffMappingRequest } from '@/modules/division/type/division.d';

/**
 * HU-38: one stage within a {@link ICreateFullDivisionRequest}. Mirrors the
 * backend `CreateFullStageRequest` DTO (the granular add-stage payload minus
 * the DivisionId, which is implied by nesting).
 * @interface ICreateFullStageRequest
 */
export interface ICreateFullStageRequest {
  /** The name of the stage (e.g. "Fase de Grupos", "Copa de Oro - Final"). */
  name: string;

  /** Optional description providing additional details about the stage. */
  description?: string | null;

  /** The type of the stage (Group / RoundOf16 / QuarterFinal / …). */
  stageType: StageType;

  /** Defaults to true server-side when omitted. */
  isActive?: boolean | null;

  /** Defaults to `(stageType !== Group)` server-side when omitted. */
  isElimination?: boolean | null;

  /** The starting date of the stage. */
  startDate: Date;

  /** The ending date of the stage. */
  endDate: Date;

  /**
   * Groups parallel elimination brackets under a cup name (e.g. "Copa de Oro").
   * Omit for the division's single/default bracket.
   */
  bracketName?: string | null;

  /** Games in a series for this round (1, 3, 5 or 7). Defaults to 1. */
  bestOf?: number;

  /** Round-robin legs for a Group stage (1 = single, 2 = double, …). Defaults to 1. */
  roundRobinLegs?: number;
}

/**
 * HU-38: one division (zone or cross-division cup) within a
 * {@link ICreateFullTournamentRequest}. The TournamentId is implied by nesting.
 * Mirrors the backend `CreateFullDivisionRequest` DTO.
 * @interface ICreateFullDivisionRequest
 */
export interface ICreateFullDivisionRequest {
  /** The name of the division/zone. */
  name: string;

  /** Marks a cross-division cup that draws teams from every zone. Defaults to false. */
  isCrossDivisionCup?: boolean;

  /** Points awarded for a win in this division's standings (HU-79). Defaults to 2. */
  pointsForWin?: number;

  /** Points awarded for a loss in this division's standings (HU-79). Defaults to 1. */
  pointsForLoss?: number;

  /**
   * For a cross-division cup (HU-110): how many teams advance from EACH internal
   * group into the pooled knockout bracket. Only meaningful when
   * `isCrossDivisionCup` is true. Defaults to 1.
   */
  qualifiersPerGroup?: number;

  /**
   * Competitive category (gender) of the division (HU-48). MUST match the parent
   * tournament's category — a mismatch aborts the whole atomic create.
   */
  category?: TournamentCategory;

  /** Optional position-range → playoff-destination mappings (HU-45). */
  playoffMappings?: PlayoffMappingRequest[];

  /** The stages (group + cup elimination rounds) to create in this division. */
  stages: ICreateFullStageRequest[];
}

/**
 * HU-38: the whole tournament-wizard payload persisted atomically by
 * `POST /api/tournaments/full`. Carries the tournament base fields plus every
 * division (zone/cross-cup) with its cups, points, playoff mappings and stages.
 * The entire graph is created in a single transaction, so a failure leaves NO
 * partial tournament behind — and the backend creates it already
 * `OpenForRegistration`, so no separate open-registration call is needed.
 * Mirrors the backend `CreateFullTournamentRequest` DTO.
 * @interface ICreateFullTournamentRequest
 */
export interface ICreateFullTournamentRequest {
  /** The name of the tournament. */
  name: string;

  /** A description of the tournament. */
  description: string;

  /** The deadline for team registrations. Must be earlier than the start date. */
  teamRegistrationDeadline: Date;

  /** The start date of the tournament. */
  startDate: Date;

  /**
   * Competitive category (gender) of the tournament (HU-48). Every division
   * below must share this category. Omit to let the backend default to Masculine.
   */
  category?: TournamentCategory;

  /**
   * Optional season ("Temporada") the tournament is grouped under. Omit/`null`
   * to leave the tournament without a season.
   */
  seasonId?: GUID | null;

  /**
   * The divisions (zones and optional cross-division cup) to create under this
   * tournament, each with its own stages and playoff mappings.
   */
  divisions: ICreateFullDivisionRequest[];
}
