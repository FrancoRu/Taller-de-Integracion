import { AxiosError } from 'axios';
import { createContext, ReactNode } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useError } from '../../error/hooks/error.hock';
import { IPlayerResponse } from '../../player/type/player';
import { GUID } from '../../core/types/types';
import { ERROR_MESSAGES } from '../../core/constants/constants';
import { playerStatisticService } from '../service/playerStatistic.service';
import {
  AddPlayerStatisticRequest,
  IPlayerStatisticContextProps,
  PutPlayerStatisticRequest,
} from '../type/playerStatistic';

export const PlayerStatisticContext = createContext<
  IPlayerStatisticContextProps | undefined
>(undefined);

export const PlayerStatisticProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const { setError } = useError();
  const queryClient = useQueryClient();

  const handleUnknownError = (error: unknown) => {
    if (error instanceof AxiosError) {
      setError(error);
      return;
    }

    setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
  };

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

  const addPlayerStatistic = async (
    playerStatistic: AddPlayerStatisticRequest
  ): Promise<IPlayerResponse | void> => {
    try {
      const response =
        await addPlayerStatisticMutation.mutateAsync(playerStatistic);
      await queryClient.invalidateQueries({ queryKey: ['playerStatistic'] });
      return response?.data as unknown as IPlayerResponse;
    } catch (error: unknown) {
      handleUnknownError(error);
    }
  };

  const putPlayerStatisticById = async (
    statisticid: GUID,
    playerStatistic: PutPlayerStatisticRequest
  ): Promise<void> => {
    try {
      await putPlayerStatisticMutation.mutateAsync({
        statisticid,
        playerStatistic,
      });
      await queryClient.invalidateQueries({ queryKey: ['playerStatistic'] });
    } catch (error: unknown) {
      handleUnknownError(error);
    }
  };

  const getPlayerStatisticById = async (
    id: GUID
  ): Promise<IPlayerResponse | void> => {
    try {
      const response = await queryClient.fetchQuery({
        queryKey: ['playerStatistic', 'byId', id],
        queryFn: async () =>
          await playerStatisticService.getPlayerStatisticById(id),
      });

      return response?.data as unknown as IPlayerResponse;
    } catch (error: unknown) {
      handleUnknownError(error);
    }
  };

  const deletePlayerStatisticById = async (id: GUID): Promise<void> => {
    try {
      await deletePlayerStatisticMutation.mutateAsync(id);
      queryClient.removeQueries({ queryKey: ['playerStatistic', 'byId', id] });
      await queryClient.invalidateQueries({ queryKey: ['playerStatistic'] });
    } catch (error: unknown) {
      handleUnknownError(error);
    }
  };

  const container = {
    addPlayerStatistic,
    putPlayerStatisticById,
    getPlayerStatisticById,
    deletePlayerStatisticById,
  };
  return (
    <PlayerStatisticContext.Provider value={container}>
      {children}
    </PlayerStatisticContext.Provider>
  );
};
