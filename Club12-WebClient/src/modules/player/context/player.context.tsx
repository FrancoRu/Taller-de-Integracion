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
import { playerService } from '@/modules/player/service/player.service';
import {
  IAddPlayerRequest,
  IPlayerContextProps,
  PlayerFiltered,
  IPlayerResponse,
  IPutPlayerRequest,
} from '@/modules/player/type/player.d';
import { upsertListById } from '@/modules/core/utils/synchronizeStates';
import { ERROR_MESSAGES } from '@/modules/core/constants/constants';

export const PlayerContext = createContext<IPlayerContextProps | undefined>(
  undefined
);

export const PlayerProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [player, setPlayer] = useState<IPlayerResponse | null>(null);
  const [players, setPlayers] = useState<IPlayerResponse[] | null>(null);

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

  const addPlayerMutation = useMutation({
    mutationFn: playerService.addPlayer,
  });

  const putPlayerMutation = useMutation({
    mutationFn: ({ id, player }: { id: GUID; player: IPutPlayerRequest }) =>
      playerService.putPlayerById(id, player),
  });

  const deletePlayerMutation = useMutation({
    mutationFn: playerService.deletePlayerById,
  });

  useEffect(() => {
    if (!player) return;

    setPlayers(prev => upsertListById(prev, player));
  }, [player]);

  const addPlayer = useCallback(
    async (player: IAddPlayerRequest): Promise<IPlayerResponse | void> => {
      try {
        const res: AxiosResponse<IPlayerResponse> =
          await addPlayerMutation.mutateAsync(player);
        if (res) {
          setPlayer(res.data);
          queryClient.setQueryData(['player', 'byId', res.data.id], res);
          await queryClient.invalidateQueries({ queryKey: ['player', 'list'] });
          return res.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [addPlayerMutation, queryClient, handleUnknownError]
  );

  const getPlayerById = useCallback(
    async (
      id: GUID,
      isAdministrative: boolean = false
    ): Promise<IPlayerResponse | void> => {
      try {
        const res: AxiosResponse<IPlayerResponse> =
          await queryClient.fetchQuery({
            queryKey: ['player', 'byId', id, isAdministrative],
            queryFn: async () =>
              await playerService.getPlayerById(id, isAdministrative),
          });

        if (res) {
          setPlayer(res.data);
          return res.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [queryClient, handleUnknownError]
  );
  const getPlayersByFilter = useCallback(
    async (
      filter: PlayerFiltered
    ): Promise<GenericResponsePagination<IPlayerResponse> | void> => {
      try {
        const res = await queryClient.fetchQuery({
          queryKey: ['player', 'list', filter],
          queryFn: async () => await playerService.getPlayersByFilter(filter),
        });

        if (res?.data?.items) {
          setPlayers(res.data.items);
          return res.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [queryClient, handleUnknownError]
  );
  const putPlayerById = useCallback(
    async (
      id: GUID,
      player: IPutPlayerRequest
    ): Promise<IPlayerResponse | void> => {
      try {
        const res: AxiosResponse<IPlayerResponse> =
          await putPlayerMutation.mutateAsync({ id, player });
        if (res) {
          if (res.status === 204) {
            setPlayer(prev =>
              prev && prev.id === id ? { ...prev, ...player } : prev
            );
          } else if (res.data) {
            setPlayer(res.data);
            queryClient.setQueryData(['player', 'byId', id], res);
          }
          await queryClient.invalidateQueries({ queryKey: ['player', 'list'] });
        }
        return res.data;
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [putPlayerMutation, queryClient, handleUnknownError]
  );

  const deletePlayerById = useCallback(
    async (id: GUID): Promise<void> => {
      try {
        await deletePlayerMutation.mutateAsync(id);
        setPlayer(null);
        setPlayers(prev => prev?.filter(e => e.id !== id) ?? null);
        queryClient.removeQueries({ queryKey: ['player', 'byId', id] });
        await queryClient.invalidateQueries({ queryKey: ['player', 'list'] });
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [deletePlayerMutation, queryClient, handleUnknownError]
  );

  const container: IPlayerContextProps = useMemo(
    () => ({
      player,
      players,
      addPlayer,
      getPlayerById,
      getPlayersByFilter,
      putPlayerById,
      deletePlayerById,
    }),
    [
      player,
      players,
      addPlayer,
      getPlayerById,
      getPlayersByFilter,
      putPlayerById,
      deletePlayerById,
    ]
  );
  return (
    <PlayerContext.Provider value={container}>
      {children}
    </PlayerContext.Provider>
  );
};
