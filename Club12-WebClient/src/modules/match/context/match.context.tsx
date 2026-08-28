import { AxiosResponse } from 'axios';
import {
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
import { useUnknownErrorHandler } from '@/modules/error/hooks/useUnknownErrorHandler';
import { matchService } from '@/modules/match/service/match.service';
import {
  IAddMatchRequest,
  ILoadWalkOverRequest,
  IMatchContextProps,
  MatchFiltered,
  IMatchResponse,
  IMinimalMatchResponse,
  IPutMatchRequest,
  IPutMatchScoreRequest,
} from '@/modules/match/type/match';
import { upsertListById } from '@/modules/core/utils/synchronizeStates';
import { matchKeys } from '@/modules/match/queryKeys';

export const MatchContext = createContext<IMatchContextProps | undefined>(
  undefined
);

export const MatchProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [match, setMatch] = useState<IMatchResponse | null>(null);
  const [matches, setMatches] = useState<IMatchResponse[] | null>(null);

  const { setMessage } = useError();
  const queryClient = useQueryClient();

  const handleUnknownError = useUnknownErrorHandler();

  const addMatchMutation = useMutation({
    mutationFn: matchService.addMatch,
  });

  const putMatchScoreMutation = useMutation({
    mutationFn: ({
      id,
      matchScore,
    }: {
      id: GUID;
      matchScore: IPutMatchScoreRequest;
    }) => matchService.putMatchScoreByMatchId(id, matchScore),
  });

  const putMatchMutation = useMutation({
    mutationFn: ({
      id,
      matchDate,
    }: {
      id: GUID;
      matchDate: IPutMatchRequest;
    }) => matchService.putMatchByMatchId(id, matchDate),
  });

  const deleteMatchMutation = useMutation({
    mutationFn: matchService.deleteMatchById,
  });

  const loadWalkOverMutation = useMutation({
    mutationFn: ({
      id,
      request,
    }: {
      id: GUID;
      request: ILoadWalkOverRequest;
    }) => matchService.loadWalkOver(id, request),
  });

  const generateMatchesMutation = useMutation({
    mutationFn: matchService.generateMatches,
  });

  useEffect(() => {
    if (!match) return;

    setMatches(prev => upsertListById(prev, match));
  }, [match]);

  const addMatch = useCallback(
    async (match: IAddMatchRequest): Promise<IMinimalMatchResponse | void> => {
      try {
        const res: AxiosResponse<IMinimalMatchResponse> =
          await addMatchMutation.mutateAsync(match);
        if (res) {
          await queryClient.invalidateQueries({ queryKey: matchKeys.list() });
          setMessage(res.status, ['El partido fue creado satisfactoriamente.']);
        }
        return res.data;
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [addMatchMutation, queryClient, setMessage, handleUnknownError]
  );

  const putMatchScoreByMatchId = useCallback(
    async (
      id: GUID,
      matchScore: IPutMatchScoreRequest
    ): Promise<IMatchResponse | void> => {
      try {
        const res: AxiosResponse<IMatchResponse> =
          await putMatchScoreMutation.mutateAsync({ id, matchScore });
        if (res) {
          setMatch(res.data);
          queryClient.setQueryData(matchKeys.byId(id), res);
          await queryClient.invalidateQueries({ queryKey: matchKeys.list() });
          setMessage(res.status, ['Partido actualizado correctamente']);
        }
        return res.data;
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [putMatchScoreMutation, queryClient, setMessage, handleUnknownError]
  );

  const putMatchByMatchId = useCallback(
    async (
      id: GUID,
      matchDate: IPutMatchRequest
    ): Promise<IMatchResponse | void> => {
      try {
        const res: AxiosResponse<IMatchResponse> =
          await putMatchMutation.mutateAsync({ id, matchDate });
        if (res) {
          setMatch(res.data);
          queryClient.setQueryData(matchKeys.byId(id), res);
          await queryClient.invalidateQueries({ queryKey: matchKeys.list() });
          setMessage(res.status, ['Partido creado satisfactoriamente']);
        }
        return res.data;
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [putMatchMutation, queryClient, setMessage, handleUnknownError]
  );

  const loadWalkOver = useCallback(
    async (
      id: GUID,
      request: ILoadWalkOverRequest
    ): Promise<IMatchResponse | void> => {
      try {
        const res: AxiosResponse<IMatchResponse> =
          await loadWalkOverMutation.mutateAsync({ id, request });
        if (res) {
          setMatch(res.data);
          queryClient.setQueryData(matchKeys.byId(id), res);
          await queryClient.invalidateQueries({ queryKey: matchKeys.list() });
          setMessage(res.status, ['Walkover cargado correctamente']);
        }
        return res.data;
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [loadWalkOverMutation, queryClient, setMessage, handleUnknownError]
  );

  const getMatchById = useCallback(
    async (idOrSlug: string): Promise<IMatchResponse | void> => {
      try {
        const res: AxiosResponse<IMatchResponse> = await queryClient.fetchQuery(
          {
            queryKey: matchKeys.byId(idOrSlug),
            queryFn: async () => await matchService.getMatchById(idOrSlug),
          }
        );

        if (res) {
          setMatch(res.data);
        }
        return res.data;
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [queryClient, handleUnknownError]
  );

  const getMatchByFilter = useCallback(
    async (
      filter: MatchFiltered
    ): Promise<GenericResponsePagination<IMatchResponse> | void> => {
      try {
        const res = await queryClient.fetchQuery({
          queryKey: matchKeys.list(filter),
          queryFn: async () => await matchService.getMatchByFilter(filter),
        });

        if (res?.data?.items) {
          setMatches(res.data.items);
          return res.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [queryClient, handleUnknownError]
  );

  const deleteMatchById = useCallback(
    async (id: GUID): Promise<void> => {
      try {
        await deleteMatchMutation.mutateAsync(id);
        setMatches(prev => prev?.filter(e => e.id !== id) ?? null);
        if (match?.id === id) {
          setMatch(null);
        }
        queryClient.removeQueries({ queryKey: matchKeys.byId(id) });
        await queryClient.invalidateQueries({ queryKey: matchKeys.list() });
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [deleteMatchMutation, match, queryClient, handleUnknownError]
  );

  const generateMatchesAutomatically = useCallback(
    async (id: GUID): Promise<boolean> => {
      try {
        const res: AxiosResponse<IMatchResponse[]> =
          await generateMatchesMutation.mutateAsync(id);
        if (res) {
          setMatches(prev => [...(prev ?? []), ...res.data]);
          await queryClient.invalidateQueries({ queryKey: matchKeys.list() });
          setMessage(res.status, ['Partidos generados correctamente']);
        }
        return true;
      } catch (error: unknown) {
        handleUnknownError(error);
        return false;
      }
    },
    [generateMatchesMutation, queryClient, setMessage, handleUnknownError]
  );

  const container: IMatchContextProps = useMemo(
    () => ({
      match,
      matches,
      addMatch,
      putMatchByMatchId,
      putMatchScoreByMatchId,
      loadWalkOver,
      getMatchById,
      getMatchByFilter,
      deleteMatchById,
      generateMatchesAutomatically,
    }),
    [
      match,
      matches,
      addMatch,
      putMatchByMatchId,
      putMatchScoreByMatchId,
      loadWalkOver,
      getMatchById,
      getMatchByFilter,
      deleteMatchById,
      generateMatchesAutomatically,
    ]
  );
  return (
    <MatchContext.Provider value={container}>{children}</MatchContext.Provider>
  );
};
