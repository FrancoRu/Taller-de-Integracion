import { Filtered, GUID } from '@/modules/core/types/types';

/**
 * Represents the context properties and operations available for managing stages.
 */
export interface IStageContextProps {
  /**
   * The currently selected stage or null if none is selected.
   */
  stage: IStageResponse | null;

  /**
   * The list of all stages or null if not loaded.
   */
  stages: IStageResponse[] | null;

  /**
   * Adds a new stage.
   * @param {IAddStageRequest} division - The data required to create a new stage.
   * @returns {Promise<IStageResponse | void>} A promise resolving to the created stage or void on failure.
   */
  addStage(stage: IAddStageRequest): Promise<IStageResponse | void>;

  /**
   * Updates an existing stage by its ID.
   * @param {GUID} id - The unique identifier of the stage to update.
   * @param {IPutStageRequest} division - The updated stage data.
   * @returns {Promise<boolean | void>} A promise resolving to true if the update succeeded, or void on failure.
   */
  putStageById(id: GUID, division: IPutStageRequest): Promise<boolean | void>;

  /**
   * Retrieves a stage by its ID.
   * @param {GUID} id - The unique identifier of the stage to retrieve.
   * @returns {Promise<IStageResponse | void>} A promise resolving to the requested stage or void on failure.
   */
  getStageById(id: GUID): Promise<IStageResponse | void>;

  /**
   * Retrieves a paginated list of stages based on filters.
   * @param {StageFiltered} filter - The filter criteria for fetching stages.
   * @returns {Promise<GenericResponsePagination<IStageResponse> | void>} A promise resolving to the paginated list or void on failure.
   */
  getStagesByFilters(
    filter: StageFiltered
  ): Promise<GenericResponsePagination<IStageResponse> | void>;

  /**
   * Deletes a stage by its ID.
   * @param {GUID} id - The unique identifier of the stage to delete.
   * @returns {Promise<void>} A promise that resolves when the stage is deleted.
   */
  deleteStagesById(id: GUID): Promise<void>;

  /**
   * Generates division stages automatically based on the provided division ID and number of teams.
   *
   * @param {GUID} id - The unique identifier of the division.
   * @returns {Promise<IStageResponse[] | void>} A promise that resolves to true if the stages are successfully generated, otherwise false.
   */
  generateStagesAutomatically(id: GUID): Promise<IStageResponse[] | void>;
}

/**
 * Represents the response data for a stage.
 */
export interface IStageResponse {
  /**
   * The unique identifier of the stage.
   * @type {GUID}
   */
  id: GUID;

  /**
   * The name of the stage.
   * @type {string}
   */
  name: string;

  /**
   * Optional description providing additional details about the stage.
   * @type {string | null}
   */
  description?: string | null;

  /**
   * The type of the stage (e.g., "Group", "QuarterFinal").
   * @type {string}
   */
  stageType: StageType;

  /**
   * Indicates whether the stage is currently active.
   * @type {boolean}
   */
  isActive: boolean;

  /**
   * Indicates whether this stage is an elimination stage.
   * @type {boolean}
   */
  isElimination: boolean;

  /**
   * The start date of the stage.
   * @type {string} ISO 8601 format date.
   */
  startDate: string;

  /**
   * The end date of the stage.
   * @type {string} ISO 8601 format date.
   */
  endDate: string;

  /**
   * The ID of the division to which the stage belongs.
   * @type {GUID}
   */
  divisionId: GUID;

  /**
   * The order of the stage within the stage.
   * @type {number}
   */
  order: number;
}

/**
 * Represents the payload for updating an existing stage.
 */
export interface IPutStageRequest {
  /**
   * Optional description providing additional details about the stage.
   * @type {string | null}
   */
  description?: string | null;

  /**
   * Indicates whether the stage is currently active.
   * @type {boolean | null}
   */
  isActive?: boolean | null;
}

/**
 * Represents the payload for creating a new stage.
 */
export interface IAddStageRequest {
  /**
   * The name of the stage (e.g., "Group A", "Quarterfinals").
   * @type {string}
   */
  name: string;

  /**
   * Optional description providing additional details about the stage.
   * @type {string | null}
   */
  description?: string | null;

  /**
   * The type of the stage, such as "Group" or "Elimination".
   * @type {StageType}
   */
  stageType: StageType;

  /**
   * Indicates whether the stage is currently active.
   * Defaults to true if not provided.
   * @type {boolean | null}
   */
  isActive?: boolean | null;

  /**
   * Indicates whether this stage is an elimination stage.
   * Defaults to false if not provided.
   * @type {boolean | null}
   */
  isElimination?: boolean | null;

  /**
   * The starting date of the stage.
   * @type {Date}
   */
  startDate: Date;

  /**
   * The ending date of the stage.
   * @type {Date}
   */
  endDate: Date;

  /**
   * The ID of the division to which this stage belongs.
   * @type {GUID}
   */
  divisionId: GUID;
}
/**
 * Represents the filter and pagination parameters
 * to retrieve a filtered list of stages.
 */
export interface StageFiltered extends Filtered {
  /**
   * Optional filter by Tournament unique identifier.
   * @type {GUID | null}
   */
  tournamentId?: GUID | null;

  /**
   * Optional filter by Division unique identifier.
   * @type {GUID | null}
   */
  divisionId?: GUID | null;

  /**
   * Optional filter by stage type (e.g., "Group", "QuarterFinal").
   * @type {StageType | null}
   */
  stageType?: StageType | null;

  /**
   * Optional filter by active status.
   * @type {boolean | null}
   */
  isActive?: boolean | null;

  /**
   * Optional filter to specify if the stage is elimination or not.
   * @type {boolean | null}
   */
  isElimination?: boolean | null;

  /**
   * Optional filter by stage name (supports partial matching).
   * @type {string | null}
   */
  name?: string | null;

  /**
   * Optional filter to get stages starting on or after this date (ISO 8601 format).
   * @type {string | null}
   */
  startDate?: string | null;

  /**
   * Optional filter to get stages ending on or before this date (ISO 8601 format).
   * @type {string | null}
   */
  endDate?: string | null;
}

/**
 * Represents the different types of stages in a tournament.
 */
export enum StageType {
  /**
   * A group stage, typically used for round-robin matches.
   */
  Group = 'Group',

  /**
   * A knockout stage with 8 teams (Quarterfinals).
   */
  QuarterFinal = 'QuarterFinal',

  /**
   * A knockout stage with 4 teams (Semifinals).
   */
  SemiFinal = 'SemiFinal',

  /**
   * A match to determine the third-place winner.
   */
  ThirdPlace = 'ThirdPlace',

  /**
   * The final match of the tournament.
   */
  Final = 'Final',
}

export interface IDashboardStage {
  stages: IStageResponse[];
}

export interface IStageCreateFormState {
  name: string;
  description: string;
  stageType: StageType;
  startDate: string;
  endDate: string;
  isActive: boolean;
  isElimination: boolean;
  divisionId: GUID | '';
}

export interface IStageEditFormState {
  description: string;
  isActive: boolean;
}

export interface IStageListFilters
  extends Pick<StageFiltered, 'name' | 'tournamentId' | 'isActive'> {
  divisionId?: GUID;
}
