import { AxiosError } from "axios";
import { createContext, ReactNode } from "react";
import { GenericResponsePagination } from "../../core/types/types";
import { useError } from "../../error/hooks/error.hock";
import { matchService } from "../service/match.service";
import {
  AddMatchRequest,
  IMatchContextProps,
  MatchFiltered,
  MatchResponse,
  PutMatchDateRequest,
  PutMatchScoreRequest,
} from "../type/match";

export const MatchContext = createContext<IMatchContextProps | undefined>(
  undefined
);

export const MatchProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const { setError } = useError();

  const addMatch = async (
    match: AddMatchRequest
  ): Promise<MatchResponse | void> => {
    try {
      await matchService.addMatch(match);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
      }
    }
  };

  const putMatchScoreByMatchId = async (
    id: string,
    matchScore: PutMatchScoreRequest
  ): Promise<void> => {
    try {
      await matchService.putMatchScoreByMatchId(id, matchScore);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
      }
    }
  };

  const putMatchDateByMatchId = async (
    id: string,
    matchDate: PutMatchDateRequest
  ): Promise<void> => {
    try {
      await matchService.putMatchDateByMatchId(id, matchDate);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
      }
    }
  };

  const getMatchById = async (id: string): Promise<MatchResponse | void> => {
    try {
      await matchService.getMatchById(id);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
      }
    }
  };

  const getMatchByFilter = async (
    filter: MatchFiltered
  ): Promise<GenericResponsePagination<MatchResponse> | void> => {
    try {
      await matchService.getMatchByFilter(filter);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
      }
    }
  };
  const deleteMatchById = async (id: string): Promise<void> => {
    try {
      await matchService.deleteMatchById(id);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
      }
    }
  };

  const container: IMatchContextProps = {
    addMatch,
    putMatchDateByMatchId,
    putMatchScoreByMatchId,
    getMatchById,
    getMatchByFilter,
    deleteMatchById,
  };
  return (
    <MatchContext.Provider value={container}>{children}</MatchContext.Provider>
  );
};
