import { AxiosError, AxiosResponse } from 'axios';
import {
  createContext,
  ReactNode,
  useEffect,
  useState,
  useCallback,
  useMemo,
} from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
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

export const TeamContext = createContext<ITeamContextProps | undefined>(
  undefined
);

export const TeamProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [team, setTeam] = useState<ITeamResponse | null>(null);
  const [teams, setTeams] = useState<ITeamResponse[] | null>(null);

  const { setError } = useError();
  const queryClient = useQueryClient();

  const handleUnknownError = (error: unknown) => {
    if (error instanceof AxiosError) {
      setError(error);
      return;
    }

    setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
  };

  const addTeamMutation = useMutation({
    mutationFn: teamService.addTeam,
  });

  const putTeamMutation = useMutation({
    mutationFn: ({ id, data }: { id: GUID; data: IPutTeamRequest }) =>
      teamService.putTeamById(id, data),
  });

  const putTeamLogoMutation = useMutation({
    mutationFn: ({ id, logo }: { id: GUID; logo: File }) =>
      teamService.putTeamLogoById(id, logo),
  });

  const deleteTeamMutation = useMutation({
    mutationFn: teamService.deleteTeamById,
  });

  useEffect(() => {
    if (!team) return;
    setTeams(prev => upsertListById(prev, team));
  }, [team]);

  const addTeam = useCallback(
    async (teamData: IAddTeamRequest): Promise<ITeamResponse | void> => {
      try {
        const res: AxiosResponse<ITeamResponse> =
          await addTeamMutation.mutateAsync(teamData);
        if (res) {
          setTeam(res.data);
          queryClient.setQueryData(['team', 'byId', res.data.id], res);
          await queryClient.invalidateQueries({ queryKey: ['team', 'list'] });
        }
        return res?.data;
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [addTeamMutation, queryClient]
  );

  const putTeamById = useCallback(
    async (id: GUID, data: IPutTeamRequest): Promise<ITeamResponse | void> => {
      try {
        const res: AxiosResponse<ITeamResponse> =
          await putTeamMutation.mutateAsync({
            id,
            data,
          });

        if (res) {
          setTeam(res.data);
          queryClient.setQueryData(['team', 'byId', id], res);
          await queryClient.invalidateQueries({ queryKey: ['team', 'list'] });
        }
        return res?.data;
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [putTeamMutation, queryClient]
  );

  const putTeamLogoById = useCallback(
    async (id: GUID, logo: File): Promise<void> => {
      try {
        await putTeamLogoMutation.mutateAsync({ id, logo });
        await queryClient.invalidateQueries({ queryKey: ['team', 'byId', id] });
        await queryClient.invalidateQueries({ queryKey: ['team', 'list'] });
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [putTeamLogoMutation, queryClient]
  );

  const getTeamsByFiltered = useCallback(
    async (
      filter: TeamFiltered
    ): Promise<GenericResponsePagination<ITeamResponse> | void> => {
      try {
        const res = await queryClient.fetchQuery({
          queryKey: ['team', 'list', filter],
          queryFn: async () => await teamService.getTeamsByFiltered(filter),
        });

        if (res?.data?.items) {
          setTeams(res.data.items);
          return res.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [queryClient]
  );

  const getTeamById = useCallback(
    async (id: GUID): Promise<ITeamResponse | void> => {
      try {
        const res: AxiosResponse<ITeamResponse> = await queryClient.fetchQuery({
          queryKey: ['team', 'byId', id],
          queryFn: async () => await teamService.getTeamById(id),
        });

        if (res) {
          setTeam(res.data);
          return res.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [queryClient]
  );

  const deleteTeamById = useCallback(
    async (id: GUID): Promise<void> => {
      try {
        await deleteTeamMutation.mutateAsync(id);
        setTeams(prev => prev?.filter(e => e.id !== id) ?? null);
        if (team?.id === id) {
          setTeam(null);
        }
        queryClient.removeQueries({ queryKey: ['team', 'byId', id] });
        await queryClient.invalidateQueries({ queryKey: ['team', 'list'] });
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [deleteTeamMutation, queryClient, team]
  );

  const container: ITeamContextProps = useMemo(
    () => ({
      team,
      teams,
      addTeam,
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
    ]
  );

  return (
    <TeamContext.Provider value={container}>{children}</TeamContext.Provider>
  );
};
