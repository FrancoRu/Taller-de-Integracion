import { AxiosError } from "axios";
import { createContext, ReactNode } from "react";
import { GenericResponsePagination } from "../../core/types/types";
import { useError } from "../../error/hooks/error.hock";
import { teamService } from "../service/team.service";
import {
  AddTeamRequest,
  ITeamContextProps,
  PutTeamRequest,
  TeamFiltered,
  TeamResponse,
} from "../type/team.d";

export const TeamContext = createContext<ITeamContextProps | undefined>(
  undefined
);

export const TeamProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const { setError } = useError();
  const addTeam = async (
    team: AddTeamRequest
  ): Promise<TeamResponse | void> => {
    try {
      await teamService.addTeam(team);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
      }
    }
  };
  const addTeamToDivisionIdBatch = async (
    divisionId: string,
    teamFile: File,
    logoFile: File
  ): Promise<TeamResponse | void> => {
    try {
      await teamService.addTeamToDivisionIdBatch(
        divisionId,
        teamFile,
        logoFile
      );
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
      }
    }
  };
  const putTeamById = async (
    id: string,
    data: PutTeamRequest
  ): Promise<TeamResponse | void> => {
    try {
      await teamService.putTeamById(id, data);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
      }
    }
  };
  const putTeamLogoById = async (id: string, logo: File): Promise<void> => {
    try {
      await teamService.putTeamLogoById(id, logo);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
      }
    }
  };
  const getTeamsByFiltered = async (
    filters: TeamFiltered
  ): Promise<GenericResponsePagination<TeamResponse> | void> => {
    try {
      await teamService.getTeamsByFiltered(filters);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
      }
    }
  };
  const getTeamById = async (id: string): Promise<TeamResponse | void> => {
    try {
      await teamService.getTeamById(id);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
      }
    }
  };
  const deleteTeamById = async (id: string): Promise<void> => {
    try {
      await teamService.deleteTeamById(id);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
      }
    }
  };

  const container: ITeamContextProps = {
    addTeam,
    addTeamToDivisionIdBatch,
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
