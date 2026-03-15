import { AxiosError, AxiosResponse } from 'axios';
import { createContext, ReactNode, useEffect, useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { GenericResponsePagination, GUID } from '../../core/types/types';
import { useError } from '../../error/hooks/error.hock';
import { matchService } from '../service/match.service';
import {
  IAddMatchRequest,
  IMatchContextProps,
  MatchFiltered,
  IMatchResponse,
  IPutMatchRequest,
  IPutMatchScoreRequest,
} from '../type/match';
import { upsertListById } from '@/modules/core/utils/synchronizeStates';
import { ERROR_MESSAGES } from '@/modules/core/constants/constants';

export const MatchContext = createContext<IMatchContextProps | undefined>(
  undefined
);

export const MatchProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [match, setMatch] = useState<IMatchResponse | null>(null);
  const [matches, setMatches] = useState<IMatchResponse[] | null>(null);

  const { setError, setMessage } = useError();
  const queryClient = useQueryClient();

  const handleUnknownError = (error: unknown) => {
    if (error instanceof AxiosError) {
      setError(error);
      return;
    }

    setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
  };

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

  useEffect(() => {
    if (!match) return;

    setMatches(prev => upsertListById(prev, match));
  }, [match]);

  const addMatch = async (
    match: IAddMatchRequest
  ): Promise<IMatchResponse | void> => {
    try {
      const res: AxiosResponse<IMatchResponse> =
        await addMatchMutation.mutateAsync(match);
      if (res) {
        setMatch(res.data);
        queryClient.setQueryData(['match', 'byId', res.data.id], res);
        await queryClient.invalidateQueries({ queryKey: ['match', 'list'] });
        setMessage(res.status, ['El partido fue creado satisfactoriamente.']);
      }
      return res.data;
    } catch (error: unknown) {
      handleUnknownError(error);
    }
  };

  const putMatchScoreByMatchId = async (
    id: GUID,
    matchScore: IPutMatchScoreRequest
  ): Promise<IMatchResponse | void> => {
    try {
      const res: AxiosResponse<IMatchResponse> =
        await putMatchScoreMutation.mutateAsync({ id, matchScore });
      if (res) {
        setMatch(res.data);
        queryClient.setQueryData(['match', 'byId', id], res);
        await queryClient.invalidateQueries({ queryKey: ['match', 'list'] });
        setMessage(res.status, ['Partido actualizado correctamente']);
      }
      return res.data;
    } catch (error: unknown) {
      handleUnknownError(error);
    }
  };

  const putMatchByMatchId = async (
    id: GUID,
    matchDate: IPutMatchRequest
  ): Promise<IMatchResponse | void> => {
    try {
      const res: AxiosResponse<IMatchResponse> =
        await putMatchMutation.mutateAsync({ id, matchDate });
      if (res) {
        setMatch(res.data);
        queryClient.setQueryData(['match', 'byId', id], res);
        await queryClient.invalidateQueries({ queryKey: ['match', 'list'] });
        setMessage(res.status, ['Partido creado satisfactoriamente']);
      }
      return res.data;
    } catch (error: unknown) {
      handleUnknownError(error);
    }
  };

  const getMatchById = async (id: GUID): Promise<IMatchResponse | void> => {
    try {
      const res: AxiosResponse<IMatchResponse> = await queryClient.fetchQuery({
        queryKey: ['match', 'byId', id],
        queryFn: async () => await matchService.getMatchById(id),
      });

      if (res) {
        setMatch(res.data);
      }
      return res.data;
    } catch (error: unknown) {
      handleUnknownError(error);
    }
  };

  const getMatchByFilter = async (
    filter: MatchFiltered
  ): Promise<GenericResponsePagination<IMatchResponse> | void> => {
    try {
      const res = await queryClient.fetchQuery({
        queryKey: ['match', 'list', filter],
        queryFn: async () => await matchService.getMatchByFilter(filter),
      });

      if (res?.data?.items) {
        setMatches(res.data.items);
        return res.data;
      }
    } catch (error: unknown) {
      handleUnknownError(error);
    }
  };
  const deleteMatchById = async (id: GUID): Promise<void> => {
    try {
      await deleteMatchMutation.mutateAsync(id);
      setMatches(prev => prev?.filter(e => e.id !== id) ?? null);
      if (match?.id === id) {
        setMatch(null);
      }
      queryClient.removeQueries({ queryKey: ['match', 'byId', id] });
      await queryClient.invalidateQueries({ queryKey: ['match', 'list'] });
    } catch (error: unknown) {
      handleUnknownError(error);
    }
  };

  const generateMatchesAutomatically = async (id: GUID): Promise<boolean> => {
    try {
      console.log(id);
      return true;
    } catch (error: unknown) {
      handleUnknownError(error);
      return false; // en caso de error
    }
  };
  const container: IMatchContextProps = {
    match,
    matches,
    addMatch,
    putMatchByMatchId,
    putMatchScoreByMatchId,
    getMatchById,
    getMatchByFilter,
    deleteMatchById,
    generateMatchesAutomatically,
  };
  return (
    <MatchContext.Provider value={container}>{children}</MatchContext.Provider>
  );
};
