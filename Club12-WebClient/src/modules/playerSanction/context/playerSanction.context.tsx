import { AxiosError, AxiosResponse } from 'axios';
import React, {
  createContext,
  ReactNode,
  useCallback,
  useEffect,
  useMemo,
  useState,
} from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { GenericResponsePagination, GUID } from '@/modules/core/types/types';
import { useError } from '@/modules/error/hooks/error.hock';
import { playerSanctionService } from '@/modules/playerSanction/service/playerSanction.service';
import {
  IAddPlayerSanction,
  IPlayerSanctionContextProps,
  IPlayerSanctionFiltered,
  IPlayerSanctionResponse,
  IPutPlayerSanction,
} from '@/modules/playerSanction/type/playerSanction.d';
import { upsertListById } from '@/modules/core/utils/synchronizeStates';
import { ERROR_MESSAGES } from '@/modules/core/constants/constants';

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

  const addPlayerSanctionMutation = useMutation({
    mutationFn: playerSanctionService.addPlayerSanction,
  });

  const putPlayerSanctionMutation = useMutation({
    mutationFn: ({
      id,
      sanction,
    }: {
      id: GUID;
      sanction: IPutPlayerSanction;
    }) => playerSanctionService.putPlayerSanctionById(id, sanction),
  });

  const deletePlayerSanctionMutation = useMutation({
    mutationFn: playerSanctionService.deletePlayerSanction,
  });

  useEffect(() => {
    if (!playerSanction) return;
    setPlayerSanctions(prev => upsertListById(prev, playerSanction));
  }, [playerSanction]);

  const addPlayerSanction = useCallback(
    async (
      sanction: IAddPlayerSanction
    ): Promise<IPlayerSanctionResponse | void> => {
      try {
        const res: AxiosResponse<IPlayerSanctionResponse> =
          await addPlayerSanctionMutation.mutateAsync(sanction);
        if (res?.data) {
          setPlayerSanction(res.data);
          queryClient.setQueryData(
            ['playerSanction', 'byId', res.data.id],
            res
          );
          await queryClient.invalidateQueries({
            queryKey: ['playerSanction', 'list'],
          });
          return res.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [addPlayerSanctionMutation, queryClient, handleUnknownError]
  );

  const getPlayerSanctionById = useCallback(
    async (id: GUID): Promise<IPlayerSanctionResponse | void> => {
      try {
        const res: AxiosResponse<IPlayerSanctionResponse> =
          await queryClient.fetchQuery({
            queryKey: ['playerSanction', 'byId', id],
            queryFn: async () =>
              await playerSanctionService.getPlayerSanctionById(id),
          });

        if (res) {
          setPlayerSanction(res.data);
        }
        return res.data;
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [queryClient, handleUnknownError]
  );

  const getPlayerSanctionByFilter = useCallback(
    async (
      filter: IPlayerSanctionFiltered
    ): Promise<GenericResponsePagination<IPlayerSanctionResponse> | void> => {
      try {
        const res = await queryClient.fetchQuery({
          queryKey: ['playerSanction', 'list', filter],
          queryFn: async () =>
            await playerSanctionService.getPlayerSanctionByFilter(filter),
        });

        if (res?.data?.items) {
          setPlayerSanctions(res.data.items);
          return res.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [queryClient, handleUnknownError]
  );

  const putPlayerSanctionById = useCallback(
    async (
      id: GUID,
      sanction: IPutPlayerSanction
    ): Promise<IPlayerSanctionResponse | void> => {
      try {
        const res: AxiosResponse<IPlayerSanctionResponse> =
          await putPlayerSanctionMutation.mutateAsync({ id, sanction });
        if (res) {
          if (res.status === 204) {
            setPlayerSanction(prev =>
              prev && prev.id === id ? { ...prev, ...sanction } : prev
            );
          } else if (res.data) {
            setPlayerSanction(res.data);
            queryClient.setQueryData(['playerSanction', 'byId', id], res);
          }
          await queryClient.invalidateQueries({
            queryKey: ['playerSanction', 'list'],
          });
        }
        return res.data;
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [putPlayerSanctionMutation, queryClient, handleUnknownError]
  );

  const deletePlayerSanction = useCallback(
    async (id: GUID): Promise<void> => {
      try {
        await deletePlayerSanctionMutation.mutateAsync(id);
        setPlayerSanction(null);
        setPlayerSanctions(prev => prev?.filter(e => e.id !== id) ?? null);
        queryClient.removeQueries({ queryKey: ['playerSanction', 'byId', id] });
        await queryClient.invalidateQueries({
          queryKey: ['playerSanction', 'list'],
        });
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [deletePlayerSanctionMutation, queryClient, handleUnknownError]
  );

  const container: IPlayerSanctionContextProps = useMemo(
    () => ({
      playerSanction,
      playerSanctions,
      addPlayerSanction,
      getPlayerSanctionById,
      getPlayerSanctionByFilter,
      putPlayerSanctionById,
      deletePlayerSanction,
    }),
    [
      playerSanction,
      playerSanctions,
      addPlayerSanction,
      getPlayerSanctionById,
      getPlayerSanctionByFilter,
      putPlayerSanctionById,
      deletePlayerSanction,
    ]
  );

  return (
    <PlayerSanctionContext.Provider value={container}>
      {children}
    </PlayerSanctionContext.Provider>
  );
};
