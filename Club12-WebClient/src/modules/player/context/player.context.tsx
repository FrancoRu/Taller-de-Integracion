import { AxiosError, AxiosResponse } from 'axios';
import React, { createContext, ReactNode, useEffect, useState } from 'react';
import { GenericResponsePagination, GUID } from '../../core/types/types';
import { useError } from '../../error/hooks/error.hock';
import { playerService } from '../service/player.service';
import {
  IAddPlayerRequest,
  IPlayerContextProps,
  PlayerFiltered,
  IPlayerResponse,
  IPutPlayerRequest,
} from '../type/player.d';
import { upsertListById } from '@/modules/core/utils/synchronizeStates';

export const PlayerContext = createContext<IPlayerContextProps | undefined>(
  undefined
);

export const PlayerProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [player, setPlayer] = useState<IPlayerResponse | null>(null);
  const [players, setPlayers] = useState<IPlayerResponse[] | null>(null);

  const { setError } = useError();

  useEffect(() => {
    if (!player) return;

    setPlayers(prev => upsertListById(prev, player));
  }, [player]);

  const addPlayer = async (
    player: IAddPlayerRequest
  ): Promise<IPlayerResponse | void> => {
    try {
      const res: AxiosResponse<IPlayerResponse> =
        await playerService.addPlayer(player);
      if (res) {
        setPlayer(res.data);
      }
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };
  const getPlayerById = async (id: GUID): Promise<IPlayerResponse | void> => {
    try {
      await playerService.getPlayerById(id);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };
  const getPlayersByFilter = async (
    filter: PlayerFiltered
  ): Promise<GenericResponsePagination<IPlayerResponse> | void> => {
    try {
      await playerService.getPlayersByFilter(filter);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };
  const putPlayerById = async (
    id: GUID,
    player: IPutPlayerRequest
  ): Promise<IPlayerResponse | void> => {
    try {
      const res: AxiosResponse<IPlayerResponse> =
        await playerService.putPlayerById(id, player);
      if (res) {
        setPlayer(res.data);
      }
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const deletePlayerById = async (id: GUID): Promise<void> => {
    try {
      await playerService.deletePlayerById(id);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const container: IPlayerContextProps = {
    player,
    players,
    addPlayer,
    getPlayerById,
    getPlayersByFilter,
    putPlayerById,
    deletePlayerById,
  };
  return (
    <PlayerContext.Provider value={container}>
      {children}s
    </PlayerContext.Provider>
  );
};
