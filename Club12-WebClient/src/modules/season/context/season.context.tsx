import { AxiosResponse } from 'axios';
import {
  createContext,
  ReactNode,
  useEffect,
  useState,
  useCallback,
  useMemo,
} from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useError } from '@/modules/error/hooks/error.hock';
import { useUnknownErrorHandler } from '@/modules/error/hooks/useUnknownErrorHandler';
import { seasonService } from '@/modules/season/service/season.service';
import {
  IAddSeasonRequest,
  ISeasonContextProps,
  IPutSeasonRequest,
  ISeasonResponse,
  SeasonFiltered,
} from '@/modules/season/type/season';
import {
  GenericResponsePagination,
  GUID,
} from '@/modules/core/types/types';
import { upsertListById } from '@/modules/core/utils/synchronizeStates';
import { seasonKeys } from '@/modules/season/queryKeys';
import { HttpStatus } from '@/modules/core/constants/httpStatus';

export const SeasonContext = createContext<ISeasonContextProps | undefined>(
  undefined
);

export const SeasonProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [season, setSeason] = useState<ISeasonResponse | null>(null);
  const [seasons, setSeasons] = useState<ISeasonResponse[] | null>(null);

  const { setMessage } = useError();
  const queryClient = useQueryClient();

  const handleUnknownError = useUnknownErrorHandler();

  const addSeasonMutation = useMutation({
    mutationFn: seasonService.addSeason,
  });

  const putSeasonMutation = useMutation({
    mutationFn: ({ id, season }: { id: GUID; season: IPutSeasonRequest }) =>
      seasonService.putSeasonById(id, season),
  });

  const deleteSeasonMutation = useMutation({
    mutationFn: seasonService.deleteSeasonById,
  });

  useEffect(() => {
    if (!season) return;
    setSeasons(prev => upsertListById(prev, season));
  }, [season]);

  const addSeason = useCallback(
    async (season: IAddSeasonRequest): Promise<ISeasonResponse | void> => {
      try {
        const res: AxiosResponse<ISeasonResponse> =
          await addSeasonMutation.mutateAsync(season);

        if (res) {
          setSeason(res.data);
          queryClient.setQueryData(seasonKeys.byId(res.data.id), res);
          await queryClient.invalidateQueries({ queryKey: seasonKeys.all });
          setMessage(res.status, ['La temporada fue creada exitosamente.']);
        }

        return res.data;
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [addSeasonMutation, queryClient, setMessage, handleUnknownError]
  );

  const putSeasonById = useCallback(
    async (
      id: GUID,
      season: IPutSeasonRequest
    ): Promise<ISeasonResponse | void> => {
      try {
        const res: AxiosResponse<ISeasonResponse> =
          await putSeasonMutation.mutateAsync({ id, season });

        if (res) {
          if (res.status === HttpStatus.NoContent) {
            const currentSeason =
              seasons?.find(existingSeason => existingSeason.id === id) ?? null;
            const updatedSeason: ISeasonResponse = {
              id,
              slug: currentSeason?.slug ?? '',
              name: season.name ?? currentSeason?.name ?? '',
              year:
                season.year !== undefined
                  ? season.year
                  : (currentSeason?.year ?? null),
              tournaments: currentSeason?.tournaments ?? [],
            };
            setSeason(updatedSeason);
            setSeasons(prev => upsertListById(prev, updatedSeason));
            await queryClient.invalidateQueries({ queryKey: seasonKeys.all });
            setMessage(res.status, [
              'La temporada fue actualizada correctamente.',
            ]);
            return updatedSeason;
          } else if (res.data) {
            setSeason(res.data);
            setSeasons(prev => upsertListById(prev, res.data));
            queryClient.setQueryData(seasonKeys.byId(id), res);
            await queryClient.invalidateQueries({ queryKey: seasonKeys.all });
            setMessage(res.status, [
              'La temporada fue actualizada correctamente.',
            ]);
            return res.data;
          }
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [putSeasonMutation, queryClient, setMessage, seasons, handleUnknownError]
  );

  const getSeasonsByFiltered = useCallback(
    async (
      filter: SeasonFiltered
    ): Promise<GenericResponsePagination<ISeasonResponse> | void> => {
      try {
        const res: AxiosResponse<GenericResponsePagination<ISeasonResponse>> =
          await queryClient.fetchQuery({
            queryKey: seasonKeys.list(filter),
            queryFn: async () => await seasonService.getSeasonsByFiltered(filter),
          });

        if (res) {
          setSeasons(res.data.items);
        }
        return res.data;
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [queryClient, handleUnknownError]
  );

  const getSeasonById = useCallback(
    async (idOrSlug: string): Promise<ISeasonResponse | void> => {
      try {
        const existingSeason = seasons?.find(
          e => e.id === idOrSlug || e.slug === idOrSlug
        );

        if (existingSeason) {
          setSeason(existingSeason);
          return existingSeason;
        }
        const res: AxiosResponse<ISeasonResponse> =
          await queryClient.fetchQuery({
            queryKey: seasonKeys.byId(idOrSlug),
            queryFn: async () => await seasonService.getSeasonById(idOrSlug),
          });

        if (res) {
          setSeason(res.data);
        }

        return res.data;
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [seasons, queryClient, handleUnknownError]
  );

  const deleteSeasonById = useCallback(
    async (id: GUID): Promise<void> => {
      try {
        await deleteSeasonMutation.mutateAsync(id);
        setSeason(null);
        setSeasons(prev => (prev ? prev.filter(e => e.id !== id) : null));
        queryClient.removeQueries({ queryKey: seasonKeys.byId(id) });
        await queryClient.invalidateQueries({ queryKey: seasonKeys.all });
        setMessage(HttpStatus.NoContent, ['La temporada ha sido eliminada.']);
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [deleteSeasonMutation, queryClient, setMessage, handleUnknownError]
  );

  const container: ISeasonContextProps = useMemo(
    () => ({
      season,
      seasons,
      addSeason,
      getSeasonsByFiltered,
      getSeasonById,
      putSeasonById,
      deleteSeasonById,
    }),
    [
      season,
      seasons,
      addSeason,
      getSeasonsByFiltered,
      getSeasonById,
      putSeasonById,
      deleteSeasonById,
    ]
  );

  return (
    <SeasonContext.Provider value={container}>
      {children}
    </SeasonContext.Provider>
  );
};
