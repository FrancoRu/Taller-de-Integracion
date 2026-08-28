import { AxiosResponse } from 'axios';
import {
  createContext,
  ReactNode,
  useCallback,
  useMemo,
  useState,
} from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useUnknownErrorHandler } from '@/modules/error/hooks/useUnknownErrorHandler';
import { GenericResponsePagination, GUID } from '@/modules/core/types/types';
import { playerStatisticService } from '@/modules/playerStatistic/service/playerStatistic.service';
import {
  AddPlayerStatisticRequest,
  IPlayerStatisticContextProps,
  LoadMatchSheetRequest,
  PlayerStatisticFiltered,
  PlayerStatisticResponse,
  PutPlayerStatisticRequest,
} from '@/modules/playerStatistic/type/playerStatistic';
import { playerStatisticKeys } from '@/modules/playerStatistic/queryKeys';

export const PlayerStatisticContext = createContext<
  IPlayerStatisticContextProps | undefined
>(undefined);

export const PlayerStatisticProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [playerStatistic, setPlayerStatistic] =
    useState<PlayerStatisticResponse | null>(null);
  const [playerStatistics, setPlayerStatistics] = useState<
    PlayerStatisticResponse[] | null
  >(null);
  const queryClient = useQueryClient();

  const handleUnknownError = useUnknownErrorHandler();

  const addPlayerStatisticMutation = useMutation({
    mutationFn: playerStatisticService.addPlayerStatistic,
  });

  const putPlayerStatisticMutation = useMutation({
    mutationFn: ({
      statisticid,
      playerStatistic,
    }: {
      statisticid: GUID;
      playerStatistic: PutPlayerStatisticRequest;
    }) =>
      playerStatisticService.putPlayerStatisticById(
        statisticid,
        playerStatistic
      ),
  });

  const deletePlayerStatisticMutation = useMutation({
    mutationFn: playerStatisticService.deletePlayerStatisticById,
  });

  const loadMatchSheetMutation = useMutation({
    mutationFn: playerStatisticService.loadMatchSheet,
  });

  const addPlayerStatistic = useCallback(
    async (
      newPlayerStatistic: AddPlayerStatisticRequest
    ): Promise<PlayerStatisticResponse | void> => {
      try {
        const response: AxiosResponse<PlayerStatisticResponse> =
          await addPlayerStatisticMutation.mutateAsync(newPlayerStatistic);

        if (response?.data) {
          setPlayerStatistic(response.data);
          queryClient.setQueryData(
            ['playerStatistic', 'byId', response.data.id],
            response
          );
          await queryClient.invalidateQueries({
            queryKey: playerStatisticKeys.all,
          });
          return response.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [addPlayerStatisticMutation, handleUnknownError, queryClient]
  );

  const putPlayerStatisticById = useCallback(
    async (
      statisticid: GUID,
      playerStatistic: PutPlayerStatisticRequest
    ): Promise<void> => {
      try {
        await putPlayerStatisticMutation.mutateAsync({
          statisticid,
          playerStatistic,
        });
        await queryClient.invalidateQueries({
          queryKey: playerStatisticKeys.all,
        });
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [putPlayerStatisticMutation, queryClient, handleUnknownError]
  );

  const getPlayerStatisticById = useCallback(
    async (id: GUID): Promise<PlayerStatisticResponse | void> => {
      try {
        const response: AxiosResponse<PlayerStatisticResponse> =
          await queryClient.fetchQuery({
            queryKey: ['playerStatistic', 'byId', id],
            queryFn: async () =>
              await playerStatisticService.getPlayerStatisticById(id),
          });

        if (response?.data) {
          setPlayerStatistic(response.data);
          return response.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [handleUnknownError, queryClient]
  );

  const getPlayerStatisticsByFilter = useCallback(
    async (
      filter: PlayerStatisticFiltered
    ): Promise<GenericResponsePagination<PlayerStatisticResponse> | void> => {
      try {
        const response = await queryClient.fetchQuery({
          queryKey: ['playerStatistic', 'list', filter],
          queryFn: async () =>
            await playerStatisticService.getPlayerStatisticsByFilter(filter),
        });

        if (response?.data?.items) {
          setPlayerStatistics(response.data.items);
          return response.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [handleUnknownError, queryClient]
  );

  const deletePlayerStatisticById = useCallback(
    async (id: GUID): Promise<void> => {
      try {
        await deletePlayerStatisticMutation.mutateAsync(id);
        queryClient.removeQueries({
          queryKey: ['playerStatistic', 'byId', id],
        });
        await queryClient.invalidateQueries({
          queryKey: playerStatisticKeys.all,
        });
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [deletePlayerStatisticMutation, queryClient, handleUnknownError]
  );

  const loadMatchSheet = useCallback(
    async (
      request: LoadMatchSheetRequest
    ): Promise<PlayerStatisticResponse[] | void> => {
      try {
        const response: AxiosResponse<PlayerStatisticResponse[]> =
          await loadMatchSheetMutation.mutateAsync(request);

        await queryClient.invalidateQueries({
          queryKey: playerStatisticKeys.all,
        });
        return response.data;
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [loadMatchSheetMutation, queryClient, handleUnknownError]
  );

  const container = useMemo(
    () => ({
      playerStatistic,
      playerStatistics,
      addPlayerStatistic,
      putPlayerStatisticById,
      getPlayerStatisticById,
      getPlayerStatisticsByFilter,
      deletePlayerStatisticById,
      loadMatchSheet,
    }),
    [
      addPlayerStatistic,
      deletePlayerStatisticById,
      getPlayerStatisticById,
      getPlayerStatisticsByFilter,
      loadMatchSheet,
      playerStatistic,
      playerStatistics,
      putPlayerStatisticById,
    ]
  );
  return (
    <PlayerStatisticContext.Provider value={container}>
      {children}
    </PlayerStatisticContext.Provider>
  );
};
