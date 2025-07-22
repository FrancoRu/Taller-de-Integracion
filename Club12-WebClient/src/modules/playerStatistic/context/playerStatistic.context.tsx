import { AxiosError } from 'axios';
import { createContext, ReactNode } from 'react';
import { useError } from '../../error/hooks/error.hock';
import { PlayerResponse } from '../../player/type/player';
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
  const addPlayerStatistic = async (
    playerStatistic: AddPlayerStatisticRequest
  ): Promise<PlayerResponse | void> => {
    try {
      await playerStatisticService.addPlayerStatistic(playerStatistic);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };
  const putPlayerStatisticById = async (
    statisticid: GUID,
    playerStatistic: PutPlayerStatisticRequest
  ): Promise<void> => {
    try {
      await playerStatisticService.putPlayerStatisticById(
        statisticId,
        playerStatistic
      );
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };
  const getPlayerStatisticById = async (
    id: GUID
  ): Promise<PlayerResponse | void> => {
    try {
      await playerStatisticService.getPlayerStatisticById(id);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const deletePlayerStatisticById = async (id: GUID): Promise<void> => {
    try {
      await playerStatisticService.deletePlayerStatisticById(id);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
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
