import { AxiosError } from 'axios';
import React, {
  createContext,
  ReactNode,
  useCallback,
  useMemo,
  useState,
} from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { GenericResponsePagination } from '@/modules/core/types/types';
import { ERROR_MESSAGES } from '@/modules/core/constants/constants';
import { useError } from '@/modules/error/hooks/error.hock';
import { scorerService } from '@/modules/scorer/service/scorer.service';
import {
  IScorerByPlayerResponse,
  IScorerByTeamFiltered,
  IScorerByTeamResponse,
  IScorerContextProps,
  IScorerFiltered,
} from '@/modules/scorer/type/scorer.d';

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
  const { setError } = useError();
  const queryClient = useQueryClient();

  const handleUnknownError = useCallback(
    (error: unknown) => {
      if (error instanceof AxiosError) {
        setError(error);
        return;
      }

      setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
    },
    [setError]
  );

  const getScorersByTeamFiltered = useCallback(
    async (
      filter: IScorerByTeamFiltered
    ): Promise<GenericResponsePagination<IScorerByTeamResponse> | void> => {
      try {
        const response = await queryClient.fetchQuery({
          queryKey: ['scorer', 'byTeam', filter],
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
          queryKey: ['scorer', 'byPlayer', filter],
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
