import { AxiosError, AxiosResponse } from 'axios';
import {
  createContext,
  useEffect,
  useState,
  useCallback,
  useMemo,
} from 'react';
import {
  FetchOptions,
  GenericResponsePagination,
  GUID,
  ProviderProps,
} from '@/modules/core/types/types';
import { useError } from '@/modules/error/hooks/error.hock';
import { tournamentService } from '@/modules/tournament/service/tournament.service';
import {
  IAddTournamentRequest,
  IEnrollTeamRequest,
  ITournamentCompletability,
  ITournamentContextProps,
  IPutTournamentRequest,
  ITournamentFiltered,
  ITournamentResponse,
  ITournamentStructureResponse,
} from '@/modules/tournament/type/tournament';
import {
  ICreateFullDivisionRequest,
  ICreateFullTournamentRequest,
} from '@/modules/tournament/type/createFullTournament.d';
import { IDivisionResponse } from '@/modules/division/type/division.d';
import { upsertListById } from '@/modules/core/utils/synchronizeStates';
import { ERROR_MESSAGES } from '@/modules/core/constants/constants';
import { fetchAndSetList } from '@/modules/core/utils/comparator';
import { HttpStatus } from '@/modules/core/constants/httpStatus';

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
    [setTournament, setError, setMessage]
  );

  const createFullTournament = useCallback(
    async (
      request: ICreateFullTournamentRequest
    ): Promise<ITournamentResponse | void> => {
      try {
        const res: AxiosResponse<ITournamentResponse> =
          await tournamentService.createFullTournament(request);

        if (res && res.data) {
          setTournament(res.data);
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

  const addFullDivision = useCallback(
    async (
      tournamentId: GUID,
      request: ICreateFullDivisionRequest
    ): Promise<IDivisionResponse | void> => {
      try {
        const res: AxiosResponse<IDivisionResponse> =
          await tournamentService.addFullDivision(tournamentId, request);

        return res.data;
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

  const putTournamentById = useCallback(
    async (
      id: GUID,
      tournamentRequest: IPutTournamentRequest
    ): Promise<boolean | void> => {
      try {
        const res: AxiosResponse<void> =
          await tournamentService.putTournamentById(id, tournamentRequest);
        if (res && res.status === HttpStatus.NoContent) {
          setTournament(prev => {
            const fallbackFromList =
              tournaments?.find(e => e.id === id) ?? null;
            const current = prev && prev.id === id ? prev : fallbackFromList;

            if (!current) {
              return prev;
            }

            return {
              ...current,
              ...tournamentRequest,
              status: tournamentRequest.status ?? current.status,
            };
          });
          setMessage(res.status, ['Torneo actualizado correctamente']);
          return true;
        }
      } catch (error: unknown) {
        if (error instanceof AxiosError) {
          setError(error);
        } else {
          setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
        }
      }
    },
    [setTournament, setError, setMessage, tournaments]
  );

  const getTournamentById = useCallback(
    async (
      id: string,
      options?: FetchOptions
    ): Promise<ITournamentResponse | void> => {
      try {
        const existTournament = tournaments?.find(
          e => e.id === id || e.slug === id
        );

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
        if (options?.silent) return;
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
      filter: ITournamentFiltered,
      options?: FetchOptions
    ): Promise<GenericResponsePagination<ITournamentResponse> | void> => {
      try {
        return await fetchAndSetList<ITournamentResponse, ITournamentFiltered>({
          apiCall: f => tournamentService.getAllTournamentsByFilter(f),
          currentState: tournaments,
          setState: setTournaments,
          filter: filter,
        });
      } catch (error: unknown) {
        if (options?.silent) return;
        if (error instanceof AxiosError) {
          setError(error);
        } else {
          setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
        }
      }
    },
    [setTournaments, setError, tournaments]
  );

  const deleteTournamentById = useCallback(
    async (id: GUID): Promise<boolean> => {
      try {
        await tournamentService.deleteTournamentById(id);
        setTournament(null);
        setTournaments(prev => (prev ? prev.filter(e => e.id !== id) : null));
        return true;
      } catch (error: unknown) {
        if (error instanceof AxiosError) {
          setError(error);
        } else {
          setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
        }
        return false;
      }
    },
    [setTournament, setTournaments, setError]
  );

  const enrollTeam = useCallback(
    async (id: GUID, request: IEnrollTeamRequest): Promise<boolean | void> => {
      try {
        const res: AxiosResponse<void> = await tournamentService.enrollTeam(
          id,
          request
        );

        if (res) {
          setMessage(res.status, ['Equipo inscripto correctamente']);
        }
        return (
          res.status === HttpStatus.Ok ||
          res.status === HttpStatus.Created ||
          res.status === HttpStatus.NoContent
        );
      } catch (error: unknown) {
        if (error instanceof AxiosError) {
          setError(error);
        } else {
          setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
        }
      }
    },
    [setError, setMessage]
  );

  const unenrollTeam = useCallback(
    async (id: GUID, teamId: GUID): Promise<boolean | void> => {
      try {
        const res: AxiosResponse<void> = await tournamentService.unenrollTeam(
          id,
          teamId
        );

        if (res) {
          setMessage(res.status, ['Equipo dado de baja correctamente']);
        }
        return (
          res.status === HttpStatus.Ok || res.status === HttpStatus.NoContent
        );
      } catch (error: unknown) {
        if (error instanceof AxiosError) {
          setError(error);
        } else {
          setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
        }
      }
    },
    [setError, setMessage]
  );

  const getCompletability = useCallback(
    async (id: GUID): Promise<ITournamentCompletability | void> => {
      try {
        const res: AxiosResponse<ITournamentCompletability> =
          await tournamentService.getCompletability(id);

        if (res && res.data) {
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
    [setError]
  );

  const getStructure = useCallback(
    async (idOrSlug: string): Promise<ITournamentStructureResponse | void> => {
      try {
        const res: AxiosResponse<ITournamentStructureResponse> =
          await tournamentService.getStructure(idOrSlug);

        return res.data;
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
      createFullTournament,
      addFullDivision,
      getAllTournamentsByFilter,
      getTournamentById,
      putTournamentById,
      deleteTournamentById,
      enrollTeam,
      unenrollTeam,
      getCompletability,
      getStructure,
    }),
    [
      tournament,
      tournaments,
      addTournament,
      createFullTournament,
      addFullDivision,
      getAllTournamentsByFilter,
      getTournamentById,
      putTournamentById,
      deleteTournamentById,
      enrollTeam,
      unenrollTeam,
      getCompletability,
      getStructure,
    ]
  );

  return (
    <TournamentContext.Provider value={container}>
      {children}
    </TournamentContext.Provider>
  );
};
