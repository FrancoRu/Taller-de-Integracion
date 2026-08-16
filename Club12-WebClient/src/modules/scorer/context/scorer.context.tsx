import React, {
  createContext,
  ReactNode,
  useCallback,
  useMemo,
  useState,
} from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { GenericResponsePagination } from '@/modules/core/types/types';
import { useUnknownErrorHandler } from '@/modules/error/hooks/useUnknownErrorHandler';
import { scorerService } from '@/modules/scorer/service/scorer.service';
import {
  IScorerByPlayerResponse,
  IScorerByTeamFiltered,
  IScorerByTeamResponse,
  IScorerContextProps,
  IScorerFiltered,
} from '@/modules/scorer/type/scorer.d';
import { scorerKeys } from '@/modules/scorer/queryKeys';

export const ScorerContext = createContext<IScorerContextProps | undefined>(
  undefined
);

export const ScorerProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [scorersByTeam, setScorersByTeam] = useState<
    IScorerByTeamResponse[] | null
  >(null);
  const [scorersByPlayer, setScorersByPlayer] = useState<
    IScorerByPlayerResponse[] | null
  >(null);
  const queryClient = useQueryClient();

  const handleUnknownError = useUnknownErrorHandler();

  const getScorersByTeamFiltered = useCallback(
    async (
      filter: IScorerByTeamFiltered
    ): Promise<GenericResponsePagination<IScorerByTeamResponse> | void> => {
      try {
        const response = await queryClient.fetchQuery({
          queryKey: scorerKeys.byTeam(filter),
          queryFn: async () =>
            await scorerService.getScorersByTeamFiltered(filter),
        });

        if (response?.data?.items) {
          setScorersByTeam(response.data.items);
          return response.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [handleUnknownError, queryClient]
  );

  const getScorersByPlayerFiltered = useCallback(
    async (
      filter: IScorerFiltered
    ): Promise<GenericResponsePagination<IScorerByPlayerResponse> | void> => {
      try {
        const response = await queryClient.fetchQuery({
          queryKey: scorerKeys.byPlayer(filter),
          queryFn: async () =>
            await scorerService.getScorersByPlayerFiltered(filter),
        });

        if (response?.data?.items) {
          setScorersByPlayer(response.data.items);
          return response.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [handleUnknownError, queryClient]
  );

  const container: IScorerContextProps = useMemo(
    () => ({
      scorersByTeam,
      scorersByPlayer,
      getScorersByTeamFiltered,
      getScorersByPlayerFiltered,
    }),
    [
      getScorersByPlayerFiltered,
      getScorersByTeamFiltered,
      scorersByPlayer,
      scorersByTeam,
    ]
  );

  return (
    <ScorerContext.Provider value={container}>
      {children}
    </ScorerContext.Provider>
  );
};
