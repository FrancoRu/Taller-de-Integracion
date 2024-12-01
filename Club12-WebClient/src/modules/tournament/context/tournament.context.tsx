import { AxiosError } from 'axios';
import { createContext } from 'react';
import {
  GenericResponsePagination,
  ProviderProps,
} from '../../core/types/types';
import { useError } from '../../error/hooks/error.hock';
import { tournamentService } from '../service/tournament.service';
import {
  AddTournamentRequest,
  ITournamentContextProps,
  PutTournamentRequest,
  TournamentFiltered,
  TournamentResponse,
} from '../type/tournament';

export const TournamentContext = createContext<
  ITournamentContextProps | undefined
>(undefined);

export const TournamentProvider: React.FC<ProviderProps> = ({ children }) => {
  const { setError } = useError();
  const addTournament = async (
    tournament: AddTournamentRequest
  ): Promise<TournamentResponse | void> => {
    try {
      await tournamentService.addTournament(tournament);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };
  const putTournamentById = async (
    id: string,
    tournament: PutTournamentRequest
  ): Promise<void> => {
    try {
      await tournamentService.putTournamentById(id, tournament);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };
  const getTournamentById = async (
    id: string
  ): Promise<TournamentResponse | void> => {
    try {
      await tournamentService.getTournamentById(id);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };
  const getAllTournamentsByFilter = async (
    filter: TournamentFiltered
  ): Promise<GenericResponsePagination<TournamentResponse> | void> => {
    try {
      await tournamentService.getAllTournamentsByFilter(filter);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };
  const deleteTournamentById = async (id: string): Promise<void> => {
    try {
      await tournamentService.deleteTournamentById(id);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const container: ITournamentContextProps = {
    addTournament,
    getAllTournamentsByFilter,
    getTournamentById,
    putTournamentById,
    deleteTournamentById,
  };
  return (
    <TournamentContext.Provider value={container}>
      {children}
    </TournamentContext.Provider>
  );
};
