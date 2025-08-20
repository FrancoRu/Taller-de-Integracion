import { AxiosError, AxiosResponse } from 'axios';
import React, { createContext, ReactNode, useEffect, useState } from 'react';
import { GenericResponsePagination, GUID } from '../../core/types/types';
import { useError } from '../../error/hooks/error.hock';
import { playerSanctionService } from '../service/playerSanction.service';
import {
  IAddPlayerSanction,
  IPlayerSanctionContextProps,
  IPlayerSanctionFiltered,
  IPlayerSanctionResponse,
  IPutPlayerSanction,
} from '../type/playerSanction.d';
import { upsertListById } from '@/modules/core/utils/synchronizeStates';
import { ERROR_MESSAGES } from '@/modules/core/constants/constants';
import { fetchAndSetList } from '@/modules/core/utils/comparator';

export const PlayerSanctionContext = createContext<
  IPlayerSanctionContextProps | undefined
>(undefined);

export const PlayerSanctionProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [playerSanction, setPlayerSanction] =
    useState<IPlayerSanctionResponse | null>(null);
  const [playerSanctions, setPlayerSanctions] = useState<
    IPlayerSanctionResponse[] | null
  >(null);

  const { setError } = useError();

  useEffect(() => {
    if (!playerSanction) return;
    setPlayerSanctions(prev => upsertListById(prev, playerSanction));
  }, [playerSanction]);

  const removeToList = (id: GUID) => {
    setPlayerSanctions(prev => prev?.filter(e => e.id != id) ?? null);
  };
  const addPlayerSanction = async (
    sanction: IAddPlayerSanction
  ): Promise<IPlayerSanctionResponse | void> => {
    try {
      const res: AxiosResponse<IPlayerSanctionResponse> =
        await playerSanctionService.addPlayerSanction(sanction);
      if (res) {
        setPlayerSanction(res.data);
      }
      return res.data;
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
      }
    }
  };

  const getPlayerSanctionById = async (
    id: GUID
  ): Promise<IPlayerSanctionResponse | void> => {
    try {
      const res: AxiosResponse<IPlayerSanctionResponse> =
        await playerSanctionService.getPlayerSanctionById(id);

      if (res) {
        setPlayerSanction(res.data);
      }
      return res.data;
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
      }
    }
  };

  const getPlayerSanctionByFilter = async (
    filter: IPlayerSanctionFiltered
  ): Promise<GenericResponsePagination<IPlayerSanctionResponse> | void> => {
    try {
      return await fetchAndSetList<
        IPlayerSanctionResponse,
        IPlayerSanctionFiltered
      >({
        apiCall: f => playerSanctionService.getPlayerSanctionByFilter(f),
        currentState: playerSanctions,
        setState: setPlayerSanctions,
        filter: filter,
      });
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
      }
    }
  };

  const putPlayerSanctionById = async (
    id: GUID,
    sanction: IPutPlayerSanction
  ): Promise<IPlayerSanctionResponse | void> => {
    try {
      const res: AxiosResponse<IPlayerSanctionResponse> =
        await playerSanctionService.putPlayerSanctionById(id, sanction);
      if (res) {
        setPlayerSanction(res.data);
      }
      return res.data;
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
      }
    }
  };

  const deletePlayerSanction = async (id: GUID): Promise<void> => {
    try {
      await playerSanctionService.deletePlayerSanction(id);
      if (playerSanction?.id != id) {
        setPlayerSanction(playerSanctions?.find(e => e.id == id) ?? null);
      }
      setPlayerSanction(null);
      removeToList(id);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
      }
    }
  };

  const container: IPlayerSanctionContextProps = {
    playerSanction,
    playerSanctions,
    addPlayerSanction,
    getPlayerSanctionById,
    getPlayerSanctionByFilter,
    putPlayerSanctionById,
    deletePlayerSanction,
  };

  return (
    <PlayerSanctionContext.Provider value={container}>
      {children}
    </PlayerSanctionContext.Provider>
  );
};
