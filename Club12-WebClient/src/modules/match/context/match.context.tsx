import { AxiosError, AxiosResponse } from 'axios';
import { createContext, ReactNode, useEffect, useState } from 'react';
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
import { fetchAndSetList } from '@/modules/core/utils/comparator';

export const MatchContext = createContext<IMatchContextProps | undefined>(
  undefined
);

export const MatchProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [match, setMatch] = useState<IMatchResponse | null>(null);
  const [matches, setMatches] = useState<IMatchResponse[] | null>(null);

  const { setError, setMessage } = useError();

  useEffect(() => {
    if (!match) return;

    setMatches(prev => upsertListById(prev, match));
  }, [match]);

  const addMatch = async (
    match: IAddMatchRequest
  ): Promise<IMatchResponse | void> => {
    try {
      const res: AxiosResponse<IMatchResponse> =
        await matchService.addMatch(match);
      if (res) {
        setMatch(res.data);
        setMessage(res.status, ['El partido fue creado satisfactoriamente.']);
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

  const putMatchScoreByMatchId = async (
    id: GUID,
    matchScore: IPutMatchScoreRequest
  ): Promise<IMatchResponse | void> => {
    try {
      const res: AxiosResponse<IMatchResponse> =
        await matchService.putMatchScoreByMatchId(id, matchScore);
      if (res) {
        setMatch(res.data);
        setMessage(res.status, ['Partido actualizado correctamente']);
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

  const putMatchByMatchId = async (
    id: GUID,
    matchDate: IPutMatchRequest
  ): Promise<IMatchResponse | void> => {
    try {
      const res: AxiosResponse<IMatchResponse> =
        await matchService.putMatchByMatchId(id, matchDate);
      if (res) {
        setMatch(res.data);
        setMessage(res.status, ['Partido creado satisfactoriamente']);
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

  const getMatchById = async (id: GUID): Promise<IMatchResponse | void> => {
    try {
      const res: AxiosResponse<IMatchResponse> =
        await matchService.getMatchById(id);
      if (res) {
        setMatch(res.data);
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

  const getMatchByFilter = async (
    filter: MatchFiltered
  ): Promise<GenericResponsePagination<IMatchResponse> | void> => {
    try {
      return await fetchAndSetList<IMatchResponse, MatchFiltered>({
        apiCall: f => matchService.getMatchByFilter(f),
        currentState: matches,
        setState: setMatches,
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
  const deleteMatchById = async (id: GUID): Promise<void> => {
    try {
      await matchService.deleteMatchById(id);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
      }
    }
  };

  const generateMatchesAutomatically = async (id: GUID): Promise<boolean> => {
    try {
      console.log(id);
      return true;
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
      }
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
