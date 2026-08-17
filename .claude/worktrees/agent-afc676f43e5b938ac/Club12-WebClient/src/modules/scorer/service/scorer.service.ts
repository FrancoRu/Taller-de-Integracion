import { AxiosResponse } from 'axios';
import routes from '@/modules/core/constants/routes';
import { withTablePageSize } from '@/modules/core/constants/pagination';
import { MatchType } from '@/modules/core/enum/match/matchType';
import { GenericResponsePagination } from '@/modules/core/types/types';
import { sendGet } from '@/modules/core/utils/axiosUtils';
import {
  IScorerByPlayerResponse,
  IScorerByTeamFiltered,
  IScorerByTeamResponse,
  IScorerFiltered,
} from '@/modules/scorer/type/scorer.d';

export const scorerService = {
  getScorersByTeamFiltered: async (
    filter: IScorerByTeamFiltered
  ): Promise<AxiosResponse<GenericResponsePagination<IScorerByTeamResponse>>> =>
    await sendGet(
      `${routes.scorer}/by-team`,
      withTablePageSize({
        ...filter,
        type: MatchType.Regular,
      })
    ),

  getScorersByPlayerFiltered: async (
    filter: IScorerFiltered
  ): Promise<
    AxiosResponse<GenericResponsePagination<IScorerByPlayerResponse>>
  > => await sendGet(`${routes.scorer}/by-player`, withTablePageSize(filter)),
};
