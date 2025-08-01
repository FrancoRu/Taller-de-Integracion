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
  PutMatchDateRequest,
  PutMatchScoreRequest,
} from '../type/match';
import { upsertListById } from '@/modules/core/utils/synchronizeStates';

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
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const putMatchScoreByMatchId = async (
    id: GUID,
    matchScore: PutMatchScoreRequest
  ): Promise<void> => {
    try {
      await matchService.putMatchScoreByMatchId(id, matchScore);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const putMatchDateByMatchId = async (
    id: GUID,
    matchDate: PutMatchDateRequest
  ): Promise<void> => {
    try {
      await matchService.putMatchDateByMatchId(id, matchDate);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const getMatchById = async (id: GUID): Promise<IMatchResponse | void> => {
    try {
      const existingMatch: IMatchResponse | undefined = matches?.find(
        e => e.id == id
      );

      if (existingMatch) {
        match?.id !== existingMatch.id && setMatch(existingMatch);
        return existingMatch;
      }

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
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const getMatchByFilter = async (
    filter: MatchFiltered
  ): Promise<GenericResponsePagination<IMatchResponse> | void> => {
    try {
      const res: AxiosResponse<GenericResponsePagination<IMatchResponse>> =
        await matchService.getMatchByFilter(filter);
      if (res) {
        setMatches(res.data.items);
      }
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
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
        setError(new AxiosError('An unknown error occurred'));
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
        setError(new AxiosError('An unknown error occurred'));
      }
      return false; // en caso de error
    }
  };
  const container: IMatchContextProps = {
    match,
    matches,
    addMatch,
    putMatchDateByMatchId,
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
