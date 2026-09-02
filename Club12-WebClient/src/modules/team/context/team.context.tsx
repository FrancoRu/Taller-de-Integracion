import { AxiosResponse } from 'axios';
import {
  createContext,
  ReactNode,
  useEffect,
  useState,
  useCallback,
  useMemo,
} from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  FetchOptions,
  GenericResponsePagination,
  GUID,
} from '@/modules/core/types/types';
import { useUnknownErrorHandler } from '@/modules/error/hooks/useUnknownErrorHandler';
import { teamService } from '@/modules/team/service/team.service';
import {
  IAddTeamRequest,
  ITeamContextProps,
  IPutTeamRequest,
  TeamFiltered,
  ITeamResponse,
} from '@/modules/team/type/team.d';
import { upsertListById } from '@/modules/core/utils/synchronizeStates';
import { teamKeys } from '@/modules/team/queryKeys';
import { HttpStatus } from '@/modules/core/constants/httpStatus';

export const TeamContext = createContext<ITeamContextProps | undefined>(
  undefined
);

export const TeamProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [team, setTeam] = useState<ITeamResponse | null>(null);
  const [teams, setTeams] = useState<ITeamResponse[] | null>(null);

  const queryClient = useQueryClient();

  const handleUnknownError = useUnknownErrorHandler();

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
          queryClient.setQueryData(teamKeys.byId(res.data.id), res);
          await queryClient.invalidateQueries({ queryKey: teamKeys.list() });
        }
        return res?.data;
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [addTeamMutation, queryClient, handleUnknownError]
  );

  const putTeamById = useCallback(
    // Returns whether the update succeeded. A successful PUT answers 204 (no
    // body), so the caller can't rely on a returned entity — it must key off
    // this boolean to decide whether to close the dialog and refresh.
    async (id: GUID, data: IPutTeamRequest): Promise<boolean> => {
      try {
        const res: AxiosResponse<ITeamResponse> =
          await putTeamMutation.mutateAsync({
            id,
            data,
          });

        if (res) {
          if (res.status === HttpStatus.NoContent) {
            setTeam(prev =>
              prev && prev.id === id ? { ...prev, ...data } : prev
            );
          } else if (res.data) {
            setTeam(res.data);
            queryClient.setQueryData(teamKeys.byId(id), res);
          }
          await queryClient.invalidateQueries({ queryKey: teamKeys.list() });
        }
        return true;
      } catch (error: unknown) {
        handleUnknownError(error);
        return false;
      }
    },
    [putTeamMutation, queryClient, handleUnknownError]
  );

  const putTeamLogoById = useCallback(
    async (id: GUID, logo: File): Promise<void> => {
      try {
        await putTeamLogoMutation.mutateAsync({ id, logo });
        await queryClient.invalidateQueries({ queryKey: teamKeys.byId(id) });
        await queryClient.invalidateQueries({ queryKey: teamKeys.list() });
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [putTeamLogoMutation, queryClient, handleUnknownError]
  );

  const getTeamsByFiltered = useCallback(
    async (
      filter: TeamFiltered,
      options?: FetchOptions
    ): Promise<GenericResponsePagination<ITeamResponse> | void> => {
      try {
        const res = await queryClient.fetchQuery({
          queryKey: teamKeys.list(filter),
          queryFn: async () => await teamService.getTeamsByFiltered(filter),
        });

        if (res?.data?.items) {
          setTeams(res.data.items);
          return res.data;
        }
      } catch (error: unknown) {
        if (!options?.silent) handleUnknownError(error);
      }
    },
    [queryClient, handleUnknownError]
  );

  const getTeamById = useCallback(
    async (id: string, options?: FetchOptions): Promise<ITeamResponse | void> => {
      try {
        const res: AxiosResponse<ITeamResponse> = await queryClient.fetchQuery({
          queryKey: teamKeys.byId(id),
          queryFn: async () => await teamService.getTeamById(id),
        });

        if (res) {
          setTeam(res.data);
          return res.data;
        }
      } catch (error: unknown) {
        if (!options?.silent) handleUnknownError(error);
      }
    },
    [queryClient, handleUnknownError]
  );

  const deleteTeamById = useCallback(
    async (id: GUID): Promise<boolean> => {
      try {
        await deleteTeamMutation.mutateAsync(id);
        setTeams(prev => prev?.filter(e => e.id !== id) ?? null);
        if (team?.id === id) {
          setTeam(null);
        }
        queryClient.removeQueries({ queryKey: teamKeys.byId(id) });
        await queryClient.invalidateQueries({ queryKey: teamKeys.list() });
        return true;
      } catch (error: unknown) {
        handleUnknownError(error);
        return false;
      }
    },
    [deleteTeamMutation, queryClient, team, handleUnknownError]
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
