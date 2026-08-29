import { AxiosResponse } from 'axios';
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
import { useUnknownErrorHandler } from '@/modules/error/hooks/useUnknownErrorHandler';
import { playerService } from '@/modules/player/service/player.service';
import {
  IAddPlayerRequest,
  IPlayerContextProps,
  PlayerFiltered,
  IPlayerResponse,
  IPutPlayerRequest,
  IRegisterPlayerToTeamRequest,
  PlayerRegistrationResult,
} from '@/modules/player/type/player.d';
import { upsertListById } from '@/modules/core/utils/synchronizeStates';
import { playerKeys } from '@/modules/player/queryKeys';
import { HttpStatus } from '@/modules/core/constants/httpStatus';
import { mapRosterConflictMessage } from '@/modules/player/utils/rosterConflict';
import {
  extractProblemDetail,
  MutationResult,
} from '@/modules/core/utils/problemDetails';

export const PlayerContext = createContext<IPlayerContextProps | undefined>(
  undefined
);

export const PlayerProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [player, setPlayer] = useState<IPlayerResponse | null>(null);
  const [players, setPlayers] = useState<IPlayerResponse[] | null>(null);

  const queryClient = useQueryClient();

  const handleUnknownError = useUnknownErrorHandler();

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

  const registerPlayerMutation = useMutation({
    mutationFn: ({
      playerId,
      request,
    }: {
      playerId: GUID;
      request: IRegisterPlayerToTeamRequest;
    }) => playerService.registerPlayerToTeam(playerId, request),
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
          queryClient.setQueryData(playerKeys.byId(res.data.id), res);
          await queryClient.invalidateQueries({ queryKey: playerKeys.list() });
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
      idOrSlug: string,
      isAdministrative: boolean = false
    ): Promise<IPlayerResponse | void> => {
      try {
        const res: AxiosResponse<IPlayerResponse> =
          await queryClient.fetchQuery({
            queryKey: playerKeys.byId(idOrSlug, isAdministrative),
            queryFn: async () =>
              await playerService.getPlayerById(idOrSlug, isAdministrative),
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
          queryKey: playerKeys.list(filter),
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
          if (res.status === HttpStatus.NoContent) {
            setPlayer(prev =>
              prev && prev.id === id ? { ...prev, ...player } : prev
            );
          } else if (res.data) {
            setPlayer(res.data);
            queryClient.setQueryData(playerKeys.byId(id), res);
          }
          await queryClient.invalidateQueries({ queryKey: playerKeys.list() });
        }
        return res.data;
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [putPlayerMutation, queryClient, handleUnknownError]
  );

  /**
   * Unlike the read mutations, a delete can be blocked by a backend integrity
   * rule (a player with statistics/scorers/sanctions returns a 409 with a
   * Spanish message). The failure is returned as a discriminated result so the
   * caller can surface that exact reason inline instead of swallowing it.
   */
  const deletePlayerById = useCallback(
    async (id: GUID): Promise<MutationResult> => {
      try {
        await deletePlayerMutation.mutateAsync(id);
        setPlayer(null);
        setPlayers(prev => prev?.filter(e => e.id !== id) ?? null);
        queryClient.removeQueries({ queryKey: playerKeys.byId(id) });
        await queryClient.invalidateQueries({ queryKey: playerKeys.list() });
        return { success: true };
      } catch (error: unknown) {
        return {
          success: false,
          errorMessage:
            extractProblemDetail(error) ??
            'No se pudo eliminar el jugador. Intentá nuevamente.',
        };
      }
    },
    [deletePlayerMutation, queryClient]
  );

  /**
   * Unlike the other mutations, this one does not funnel a failure through the
   * global error handler: the roster invariants (HU-54) come back as a 409 the
   * caller must surface inline with the exact reason, so we translate the
   * conflict into a discriminated result instead of swallowing it. On success
   * the player/team lists are invalidated so the refreshed dorsal is picked up.
   */
  const registerPlayerToTeam = useCallback(
    async (
      playerId: GUID,
      request: IRegisterPlayerToTeamRequest
    ): Promise<PlayerRegistrationResult> => {
      try {
        const res = await registerPlayerMutation.mutateAsync({
          playerId,
          request,
        });
        await queryClient.invalidateQueries({ queryKey: playerKeys.list() });
        return { success: true, data: res.data };
      } catch (error: unknown) {
        return {
          success: false,
          errorMessage: mapRosterConflictMessage(error),
        };
      }
    },
    [registerPlayerMutation, queryClient]
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
      registerPlayerToTeam,
    }),
    [
      player,
      players,
      addPlayer,
      getPlayerById,
      getPlayersByFilter,
      putPlayerById,
      deletePlayerById,
      registerPlayerToTeam,
    ]
  );
  return (
    <PlayerContext.Provider value={container}>
      {children}
    </PlayerContext.Provider>
  );
};
