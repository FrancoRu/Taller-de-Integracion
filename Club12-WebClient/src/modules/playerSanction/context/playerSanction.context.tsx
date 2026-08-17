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
import { playerSanctionService } from '@/modules/playerSanction/service/playerSanction.service';
import {
  IAddPlayerSanction,
  IAppealPlayerSanction,
  IPlayerSanctionContextProps,
  IPlayerSanctionFiltered,
  IPlayerSanctionResponse,
  IPutPlayerSanction,
  IResolveAppeal,
} from '@/modules/playerSanction/type/playerSanction.d';
import { upsertListById } from '@/modules/core/utils/synchronizeStates';
import { playerSanctionKeys } from '@/modules/playerSanction/queryKeys';
import { HttpStatus } from '@/modules/core/constants/httpStatus';

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

  const queryClient = useQueryClient();

  const handleUnknownError = useUnknownErrorHandler();

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
            playerSanctionKeys.byId(res.data.id),
            res
          );
          await queryClient.invalidateQueries({
            queryKey: playerSanctionKeys.list(),
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
    async (idOrSlug: string): Promise<IPlayerSanctionResponse | void> => {
      try {
        const res: AxiosResponse<IPlayerSanctionResponse> =
          await queryClient.fetchQuery({
            queryKey: playerSanctionKeys.byId(idOrSlug),
            queryFn: async () =>
              await playerSanctionService.getPlayerSanctionById(idOrSlug),
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
          queryKey: playerSanctionKeys.list(filter),
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
          if (res.status === HttpStatus.NoContent) {
            setPlayerSanction(prev =>
              prev && prev.id === id ? { ...prev, ...sanction } : prev
            );
          } else if (res.data) {
            setPlayerSanction(res.data);
            queryClient.setQueryData(playerSanctionKeys.byId(id), res);
          }
          await queryClient.invalidateQueries({
            queryKey: playerSanctionKeys.list(),
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
        queryClient.removeQueries({
          queryKey: playerSanctionKeys.byId(id),
        });
        await queryClient.invalidateQueries({
          queryKey: playerSanctionKeys.list(),
        });
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [deletePlayerSanctionMutation, queryClient, handleUnknownError]
  );

  const appealPlayerSanction = useCallback(
    async (
      id: GUID,
      appeal: IAppealPlayerSanction
    ): Promise<IPlayerSanctionResponse | void> => {
      try {
        const res: AxiosResponse<IPlayerSanctionResponse> =
          await playerSanctionService.appealPlayerSanction(id, appeal);
        if (res?.data) {
          setPlayerSanction(res.data);
          queryClient.setQueryData(playerSanctionKeys.byId(id), res);
          await queryClient.invalidateQueries({
            queryKey: playerSanctionKeys.list(),
          });
          return res.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [queryClient, handleUnknownError]
  );

  const resolvePlayerSanctionAppeal = useCallback(
    async (
      id: GUID,
      resolution: IResolveAppeal
    ): Promise<IPlayerSanctionResponse | void> => {
      try {
        const res: AxiosResponse<IPlayerSanctionResponse> =
          await playerSanctionService.resolvePlayerSanctionAppeal(
            id,
            resolution
          );
        if (res?.data) {
          setPlayerSanction(res.data);
          queryClient.setQueryData(playerSanctionKeys.byId(id), res);
          await queryClient.invalidateQueries({
            queryKey: playerSanctionKeys.list(),
          });
          return res.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [queryClient, handleUnknownError]
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
      appealPlayerSanction,
      resolvePlayerSanctionAppeal,
    }),
    [
      playerSanction,
      playerSanctions,
      addPlayerSanction,
      getPlayerSanctionById,
      getPlayerSanctionByFilter,
      putPlayerSanctionById,
      deletePlayerSanction,
      appealPlayerSanction,
      resolvePlayerSanctionAppeal,
    ]
  );

  return (
    <PlayerSanctionContext.Provider value={container}>
      {children}
    </PlayerSanctionContext.Provider>
  );
};
