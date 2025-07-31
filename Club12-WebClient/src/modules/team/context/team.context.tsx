import { AxiosError, AxiosResponse } from 'axios';
import { createContext, ReactNode, useEffect, useState } from 'react';
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
    teams;
    setTeams(prev => upsertListById(prev, team));
  }, [team]);

  const addTeam = async (
    team: IAddTeamRequest
  ): Promise<ITeamResponse | void> => {
    try {
      const res: AxiosResponse<ITeamResponse> = await teamService.addTeam(team);
      if (res) {
        setTeam(res.data);
      }
      return res?.data;
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };
  // const addTeamToDivisionIdBatch = async (
  //   divisionid: GUID,
  //   teamFile: File,
  //   logoFile: File
  // ): Promise<ITeamResponse | void> => {
  //   try {
  //     await teamService.addTeamToDivisionIdBatch(
  //       divisionId,
  //       teamFile,
  //       logoFile
  //     );
  //   } catch (error: unknown) {
  //     if (error instanceof AxiosError) {
  //       setError(error);
  //     } else {
  //       setError(new AxiosError('An unknown error occurred'));
  //     }
  //   }
  // };
  const putTeamById = async (
    id: GUID,
    data: IPutTeamRequest
  ): Promise<ITeamResponse | void> => {
    try {
      await teamService.putTeamById(id, data);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const putTeamLogoById = async (id: GUID, logo: File): Promise<void> => {
    try {
      await teamService.putTeamLogoById(id, logo);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const getTeamsByFiltered = async (
    filters: TeamFiltered
  ): Promise<GenericResponsePagination<ITeamResponse> | void> => {
    try {
      const res: AxiosResponse<GenericResponsePagination<ITeamResponse>> =
        await teamService.getTeamsByFiltered(filters);
      if (res) {
        const newIds = res.data.items
          .map(e => e.id)
          .sort()
          .join(',');

        const currentIds = (teams ?? [])
          .map(t => t.id)
          .sort()
          .join(',');

        if (newIds !== currentIds) {
          setTeams(res.data.items);
        }
      }
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };
  const getTeamById = async (id: GUID): Promise<ITeamResponse | void> => {
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
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };
  const deleteTeamById = async (id: GUID): Promise<void> => {
    try {
      await teamService.deleteTeamById(id);
      setTeams(prev => prev?.filter(e => e.id !== id) ?? null);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const container: ITeamContextProps = {
    team,
    teams,
    addTeam,
    // addTeamToDivisionIdBatch,
    getTeamById,
    getTeamsByFiltered,
    putTeamById,
    putTeamLogoById,
    deleteTeamById,
  };
  return (
    <TeamContext.Provider value={container}>{children}</TeamContext.Provider>
  );
};
