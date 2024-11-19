import { AxiosError } from "axios";
import { createContext, ReactNode } from "react";
import { GenericResponsePagination } from "../../core/types/types";
import { useError } from "../../error/hooks/error.hock";
import { divisionService } from "../service/division.service";
import {
  AddDivisionRequest,
  DivisionFiltered,
  DivisionResponse,
  DivisionTopScoreResponse,
  IDivisionContextProps,
  PutDivisionRequest,
} from "../type/division";

export const DivisionContext = createContext<IDivisionContextProps | undefined>(
  undefined
);

export const DivisionProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const { setError } = useError();

  const addDivision = async (
    division: AddDivisionRequest
  ): Promise<DivisionResponse | void> => {
    try {
      await divisionService.addDivision(division);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
      }
    }
  };

  const generateFixtureByDivisionId = async (id: string): Promise<void> => {
    try {
      await divisionService.generateFixtureByDivisionId(id);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
      }
    }
  };

  const putDivisionById = async (
    id: string,
    division: PutDivisionRequest
  ): Promise<DivisionResponse | void> => {
    try {
      await divisionService.putDivisionById(id, division);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
      }
    }
  };

  const getDivisionsById = async (
    id: string
  ): Promise<DivisionResponse | void> => {
    try {
      await divisionService.getDivisionsById(id);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
      }
    }
  };

  const getDivisionsByFilters = async (
    filter: DivisionFiltered
  ): Promise<GenericResponsePagination<DivisionResponse> | void> => {
    try {
      await divisionService.getDivisionsByFilters(filter);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
      }
    }
  };

  const getTopScoresByDivisionId = async (
    id: string
  ): Promise<DivisionTopScoreResponse[] | void> => {
    try {
      await divisionService.getTopScoresByDivisionId(id);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
      }
    }
  };

  const deleteDivisionsById = async (id: string): Promise<void> => {
    try {
      await divisionService.deleteDivisionsById(id);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
      }
    }
  };

  const container: IDivisionContextProps = {
    addDivision,
    generateFixtureByDivisionId,
    putDivisionById,
    getDivisionsByFilters,
    getDivisionsById,
    getTopScoresByDivisionId,
    deleteDivisionsById,
  };

  return (
    <DivisionContext.Provider value={container}>
      {children}
    </DivisionContext.Provider>
  );
};
