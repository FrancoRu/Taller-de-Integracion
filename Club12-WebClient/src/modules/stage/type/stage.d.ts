import { GUID } from '@/modules/core/types/types.d';

export interface IStageContextProps {
  // Define your context properties here
}

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
   * Indicates whether the stage has finished.
   * @type {boolean}
   */
  isFinished: boolean;

  /**
   * The ID of the division to which the stage belongs.
   * @type {GUID}
   */
  divisionId: GUID;

  /**
   * The list of matches for the stage, grouped by week.
   * @type {MatchResponse[]}
   */
  matchesByWeek: MatchResponse[];
}

export interface IAllStagePropsView {
  stages: IStageResponse[];
}
