import { AxiosError, AxiosResponse } from 'axios';
import { createContext, ReactNode, useEffect, useState } from 'react';
import { GenericResponsePagination, GUID } from '../../core/types/types';
import { useError } from '../../error/hooks/error.hock';
import { divisionService } from '../service/division.service';
import {
  AddDivisionRequest,
  DivisionFiltered,
  IDivisionResponse,
  DivisionTopScoreResponse,
  IDivisionContextProps,
  IPutDivisionRequest,
} from '../type/division';
import { upsertListById } from '@/modules/core/utils/synchronizeStates';

export const DivisionContext = createContext<IDivisionContextProps | undefined>(
  undefined
);

export const DivisionProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [division, setDivision] = useState<IDivisionResponse | null>(null);

  const [divisions, setDivisions] = useState<IDivisionResponse[] | null>(null);

  const { setError, setMessage } = useError();

  useEffect(() => {
    if (!division) return;

    setDivisions(prev => upsertListById(prev, division));
  }, [division]);

  const addDivision = async (
    division: AddDivisionRequest
  ): Promise<IDivisionResponse | void> => {
    try {
      const res: AxiosResponse<IDivisionResponse> =
        await divisionService.addDivision(division);
      if (res && res.data) {
        setDivision(res.data);
        setMessage(res.status, ['Division creada exitosamente']);
      }
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const generateFixtureByDivisionId = async (id: GUID): Promise<void> => {
    try {
      await divisionService.generateFixtureByDivisionId(id);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const putDivisionById = async (
    id: GUID,
    divisionRequest: IPutDivisionRequest
  ): Promise<boolean | void> => {
    try {
      const res: AxiosResponse<IDivisionResponse> =
        await divisionService.putDivisionById(id, divisionRequest);

      if (res && res.status == 204) {
        setDivision(prev => {
          if (!prev) return prev;

          return {
            ...prev,
            name: divisionRequest.name,
          };
        });
        return true;
      }
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const getDivisionsById = async (
    id: GUID
  ): Promise<IDivisionResponse | void> => {
    try {
      const existinsDivision: IDivisionResponse | undefined = divisions?.find(
        e => e.id == id
      );

      if (existinsDivision) {
        setDivision(existinsDivision);
        return existinsDivision;
      }

      const res: AxiosResponse<IDivisionResponse> =
        await divisionService.getDivisionsById(id);
      if (res && res.data) {
        setDivision(res.data);
      }
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const getDivisionsByFilters = async (
    filter: DivisionFiltered
  ): Promise<GenericResponsePagination<IDivisionResponse> | void> => {
    try {
      const res: AxiosResponse<GenericResponsePagination<IDivisionResponse>> =
        await divisionService.getDivisionsByFilters(filter);
      if (res) {
        setDivisions(res.data.items);
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

  const getTopScoresByDivisionId = async (
    id: GUID
  ): Promise<DivisionTopScoreResponse[] | void> => {
    try {
      await divisionService.getTopScoresByDivisionId(id);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const deleteDivisionsById = async (id: GUID): Promise<void> => {
    try {
      await divisionService.deleteDivisionsById(id);
      setDivision(null);
      setDivisions(prev => (prev ? prev.filter(e => e.id !== id) : null));
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const container: IDivisionContextProps = {
    division,
    divisions,
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
