import { AxiosError, AxiosResponse } from 'axios';
import {
  createContext,
  useEffect,
  useState,
  useCallback,
  useMemo,
} from 'react'; // Import useCallback and useMemo
import {
  GenericResponsePagination,
  GUID,
  ProviderProps,
} from '../../core/types/types';
import { useError } from '../../error/hooks/error.hock';
import { tournamentService } from '../service/tournament.service';
import {
  IAddTournamentRequest,
  ITournamentContextProps,
  IPutTournamentRequest,
  ITournamentFiltered,
  ITournamentResponse,
} from '../type/tournament';
import { upsertListById } from '@/modules/core/utils/synchronizeStates';
import { ERROR_MESSAGES } from '@/modules/core/constants/constants';
import { fetchAndSetList } from '@/modules/core/utils/comparator';

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

  const { setError, setMessage } = useError();

  useEffect(() => {
    if (!tournament) return;

    setTournaments(prev => upsertListById(prev, tournament));
  }, [tournament]);

  const addTournament = useCallback(
    async (
      tournamentRequest: IAddTournamentRequest
    ): Promise<ITournamentResponse | void> => {
      try {
        const res: AxiosResponse<ITournamentResponse> =
          await tournamentService.addTournament(tournamentRequest);

        if (res && res.data) {
          setTournament(res.data);
          setMessage(res.status, []);
        }
        return res.data;
      } catch (error: unknown) {
        if (error instanceof AxiosError) {
          setError(error);
        } else {
          setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
        }
      }
    },
    [setTournament, setError]
  );

  const putTournamentById = useCallback(
    async (
      id: GUID,
      tournamentRequest: IPutTournamentRequest
    ): Promise<void> => {
      try {
        const res: AxiosResponse<ITournamentResponse> =
          await tournamentService.putTournamentById(id, tournamentRequest);
        if (res && res.status === 204) {
          setTournament(prev =>
            prev && prev.id === id ? { ...prev, ...tournamentRequest } : prev
          );
          setMessage(res.status, ['Torneo actualizado correctamente']);
        } else if (res && res.data) {
          setTournament(res.data);
          setMessage(res.status, []);
        }
      } catch (error: unknown) {
        if (error instanceof AxiosError) {
          setError(error);
        } else {
          setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
        }
      }
    },
    [setTournament, setError, setMessage]
  );

  const getTournamentById = useCallback(
    async (id: GUID): Promise<ITournamentResponse | void> => {
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
          setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
        }
      }
    },
    [tournaments, setTournament, setError]
  );

  const getAllTournamentsByFilter = useCallback(
    async (
      filter: ITournamentFiltered
    ): Promise<GenericResponsePagination<ITournamentResponse> | void> => {
      try {
        return await fetchAndSetList<ITournamentResponse, ITournamentFiltered>({
          apiCall: f => tournamentService.getAllTournamentsByFilter(f),
          currentState: tournaments,
          setState: setTournaments,
          filter: filter,
        });
      } catch (error: unknown) {
        if (error instanceof AxiosError) {
          setError(error);
        } else {
          setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
        }
      }
    },
    [setTournaments, setError]
  );

  const deleteTournamentById = useCallback(
    async (id: GUID): Promise<void> => {
      try {
        await tournamentService.deleteTournamentById(id);
        setTournament(null);
        setTournaments(prev => (prev ? prev.filter(e => e.id !== id) : null));
      } catch (error: unknown) {
        if (error instanceof AxiosError) {
          setError(error);
        } else {
          setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
        }
      }
    },
    [setTournament, setTournaments, setError]
  );

  const registerTeamsByTournamentId = useCallback(
    async (id: GUID, teamsId: GUID[]): Promise<boolean | void> => {
      try {
        const res: AxiosResponse<boolean> =
          await tournamentService.registerTeamsByTournamentId(id, teamsId);

        if (res) {
          setMessage(res.status, ['Registro de equipo exitoso']);
        }
        return res.status === 200 || res.status === 204;
      } catch (error: unknown) {
        if (error instanceof AxiosError) {
          setError(error);
        } else {
          setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
        }
      }
    },
    [setError]
  );

  const container: ITournamentContextProps = useMemo(
    () => ({
      tournament,
      tournaments,
      addTournament,
      getAllTournamentsByFilter,
      getTournamentById,
      putTournamentById,
      deleteTournamentById,
      registerTeamsByTournamentId,
    }),
    [
      tournament,
      tournaments,
      addTournament,
      getAllTournamentsByFilter,
      getTournamentById,
      putTournamentById,
      deleteTournamentById,
      registerTeamsByTournamentId,
    ]
  );

  return (
    <TournamentContext.Provider value={container}>
      {children}
    </TournamentContext.Provider>
  );
};
