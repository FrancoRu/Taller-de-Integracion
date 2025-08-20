import { AxiosError, AxiosResponse } from 'axios';
import {
  createContext,
  ReactNode,
  useEffect,
  useState,
  useCallback,
  useMemo,
} from 'react'; // Importamos useCallback y useMemo
import { GenericResponsePagination, GUID } from '../../core/types/types';
import { useError } from '../../error/hooks/error.hock';
import { teamService } from '../service/team.service';
import {
  IAddTeamRequest,
  ITeamContextProps,
  IPutTeamRequest,
  TeamFiltered,
  ITeamResponse,
} from '../type/team.d';
import { upsertListById } from '@/modules/core/utils/synchronizeStates';
import { ERROR_MESSAGES } from '@/modules/core/constants/constants';
import { fetchAndSetList } from '@/modules/core/utils/comparator';

export const TeamContext = createContext<ITeamContextProps | undefined>(
  undefined
);

export const TeamProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [team, setTeam] = useState<ITeamResponse | null>(null);
  const [teams, setTeams] = useState<ITeamResponse[] | null>(null);

  const { setError } = useError();

  useEffect(() => {
    if (!team) return;
    setTeams(prev => upsertListById(prev, team));
  }, [team]);

  // --- Funciones memoizadas con useCallback ---

  const addTeam = useCallback(
    async (teamData: IAddTeamRequest): Promise<ITeamResponse | void> => {
      try {
        const res: AxiosResponse<ITeamResponse> =
          await teamService.addTeam(teamData);
        if (res) {
          setTeam(res.data);
        }
        return res?.data;
      } catch (error: unknown) {
        if (error instanceof AxiosError) {
          setError(error);
        } else {
          setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
        }
      }
    },
    [setTeam, setError]
  );

  const putTeamById = useCallback(
    async (id: GUID, data: IPutTeamRequest): Promise<ITeamResponse | void> => {
      try {
        const res: AxiosResponse<ITeamResponse> = await teamService.putTeamById(
          id,
          data
        );
        if (res) {
          setTeam(res.data);
        }
      } catch (error: unknown) {
        if (error instanceof AxiosError) {
          setError(error);
        } else {
          setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
        }
      }
    },
    [setTeam, setError]
  );

  const putTeamLogoById = useCallback(
    async (id: GUID, logo: File): Promise<void> => {
      try {
        await teamService.putTeamLogoById(id, logo);
        // Podrías querer actualizar el 'team' o 'teams' después de subir un logo
        // Por ejemplo: getTeamById(id); o una lógica para actualizar el equipo en 'teams'
      } catch (error: unknown) {
        if (error instanceof AxiosError) {
          setError(error);
        } else {
          setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
        }
      }
    },
    [setError] // Dependencias: setError. Si actualizas team/teams aquí, agrégalos
  );

  const getTeamsByFiltered = useCallback(
    async (
      filter: TeamFiltered
    ): Promise<GenericResponsePagination<ITeamResponse> | void> => {
      try {
        return await fetchAndSetList<ITeamResponse, TeamFiltered>({
          apiCall: f => teamService.getTeamsByFiltered(f),
          currentState: teams,
          setState: setTeams,
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
    [setTeams, teams, setError] // Dependencias: setTeams (el setter), teams (para el `currentIds` check), setError
  );

  const getTeamById = useCallback(
    async (id: GUID): Promise<ITeamResponse | void> => {
      try {
        const res: AxiosResponse<ITeamResponse> =
          await teamService.getTeamById(id);
        if (res) {
          setTeam(res.data);
        }
      } catch (error: unknown) {
        if (error instanceof AxiosError) {
          setError(error);
        } else {
          setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
        }
      }
    },
    [setTeam, setError]
  );

  const deleteTeamById = useCallback(
    async (id: GUID): Promise<void> => {
      try {
        await teamService.deleteTeamById(id);
        setTeams(prev => prev?.filter(e => e.id !== id) ?? null);
      } catch (error: unknown) {
        if (error instanceof AxiosError) {
          setError(error);
        } else {
          setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
        }
      }
    },
    [setTeams, setError] // Dependencias: setTeams, setError
  );

  // --- Objeto de contexto memoizado con useMemo ---
  const container: ITeamContextProps = useMemo(
    () => ({
      team,
      teams,
      addTeam,
      // addTeamToDivisionIdBatch, // Comentado, por lo tanto no se incluye en las dependencias
      getTeamById,
      getTeamsByFiltered,
      putTeamById,
      putTeamLogoById,
      deleteTeamById,
    }),
    [
      team,
      teams,
      addTeam,
      getTeamById,
      getTeamsByFiltered,
      putTeamById,
      putTeamLogoById,
      deleteTeamById,
    ] // Todas las funciones y estados como dependencias
  );

  return (
    <TeamContext.Provider value={container}>{children}</TeamContext.Provider>
  );
};
