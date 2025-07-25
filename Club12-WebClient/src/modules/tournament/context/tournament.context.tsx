import { AxiosError, AxiosResponse } from 'axios';
import { createContext, useEffect, useState } from 'react';
import {
  GenericResponsePagination,
  GUID,
  ProviderProps,
} from '../../core/types/types.d';
import { useError } from '../../error/hooks/error.hock';
import { tournamentService } from '../service/tournament.service';
import {
  AddTournamentRequest,
  ITournamentContextProps,
  IPutTournamentRequest,
  ITournamentFiltered,
  ITournamentResponse,
} from '../type/tournament';
import { upsertListById } from '@/modules/core/utils/synchronizeStates';

export const TournamentContext = createContext<
  ITournamentContextProps | undefined
>(undefined);

export const TournamentProvider: React.FC<ProviderProps> = ({ children }) => {
  const [tournament, setTournament] = useState<ITournamentResponse | null>(
    null
  );

  const [tournaments, setTournaments] = useState<ITournamentResponse[] | null>(
    null
  );

  const { setError } = useError();

  useEffect(() => {
    if (!tournament) return;

    setTournaments(prev => upsertListById(prev, tournament));
  }, [tournament]);

  const addTournament = async (
    tournamentRequest: AddTournamentRequest
  ): Promise<ITournamentResponse | void> => {
    try {
      const res: AxiosResponse<ITournamentResponse> =
        await tournamentService.addTournament(tournamentRequest);

      if (res && res.data) {
        setTournament(res.data);
        return res.data;
      }
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };
  const putTournamentById = async (
    id: GUID,
    tournamentRequest: IPutTournamentRequest
  ): Promise<void> => {
    try {
      const res: AxiosResponse<ITournamentResponse> =
        await tournamentService.putTournamentById(id, tournamentRequest);
      if (res && res.status == 200) {
        setTournament(res.data);
      }
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const getTournamentById = async (
    id: GUID
  ): Promise<ITournamentResponse | void> => {
    try {
      const existTournament = tournaments?.find(e => e.id === id);

      if (existTournament) {
        setTournament(existTournament);
        return existTournament;
      }
      const res: AxiosResponse<ITournamentResponse> =
        await tournamentService.getTournamentById(id);

      if (res && res.data) {
        setTournament(res.data);
        return res.data;
      }
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const getAllTournamentsByFilter = async (
    filter: ITournamentFiltered
  ): Promise<GenericResponsePagination<ITournamentResponse> | void> => {
    try {
      const res: AxiosResponse<GenericResponsePagination<ITournamentResponse>> =
        await tournamentService.getAllTournamentsByFilter(filter);

      if (res && res.data) {
        setTournaments(res.data.items);
      }
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };
  const deleteTournamentById = async (id: GUID): Promise<void> => {
    try {
      await tournamentService.deleteTournamentById(id);
      setTournament(null);
      setTournaments(prev => (prev ? prev.filter(e => e.id !== id) : null));
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const container: ITournamentContextProps = {
    tournament,
    tournaments,
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
