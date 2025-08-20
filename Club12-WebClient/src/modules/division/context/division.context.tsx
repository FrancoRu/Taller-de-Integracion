import { AxiosError, AxiosResponse } from 'axios';
import {
  createContext,
  ReactNode,
  useEffect,
  useState,
  useCallback,
  useMemo,
} from 'react';
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
import { ERROR_MESSAGES } from '@/modules/core/constants/constants';
import { fetchAndSetList } from '@/modules/core/utils/comparator';

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

  const addDivision = useCallback(
    async (
      divisionRequest: AddDivisionRequest
    ): Promise<IDivisionResponse | void> => {
      try {
        const res: AxiosResponse<IDivisionResponse> =
          await divisionService.addDivision(divisionRequest);
        if (res && res.data) {
          setDivision(res.data);
          setMessage(res.status, ['Division creada exitosamente']);
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
    [setDivision, setMessage, setError]
  );

  const generateFixtureByDivisionId = useCallback(
    async (id: GUID): Promise<void> => {
      try {
        await divisionService.generateFixtureByDivisionId(id);
        setMessage(200, ['Fixture generado exitosamente']);
      } catch (error: unknown) {
        if (error instanceof AxiosError) {
          setError(error);
        } else {
          setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
        }
      }
    },
    [setMessage, setError]
  );

  const putDivisionById = useCallback(
    async (
      id: GUID,
      divisionRequest: IPutDivisionRequest
    ): Promise<boolean | void> => {
      try {
        const res: AxiosResponse<IDivisionResponse> =
          await divisionService.putDivisionById(id, divisionRequest);

        if (res && res.status === 204) {
          setDivision(prev => {
            if (!prev || prev.id !== id) return prev;

            return {
              ...prev,
              name: divisionRequest.name,
            };
          });
          setMessage(res.status, [
            'La información de la división fue actualizada correctamente',
          ]);
          return true;
        } else if (res && res.data) {
          setDivision(res.data);
          setMessage(res.status, [
            'La información de la división fue actualizada correctamente',
          ]);
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
    [setDivision, setMessage, setError]
  );

  const getDivisionsById = useCallback(
    async (id: GUID): Promise<IDivisionResponse | void> => {
      try {
        const existingDivision: IDivisionResponse | undefined = divisions?.find(
          e => e.id === id
        );

        if (existingDivision) {
          setDivision(existingDivision);
          return existingDivision;
        }

        const res: AxiosResponse<IDivisionResponse> =
          await divisionService.getDivisionsById(id);
        if (res && res.data) {
          setDivision(res.data);
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
    [divisions, setDivision, setError]
  );

  const getDivisionsByFilters = useCallback(
    async (
      filter: DivisionFiltered
    ): Promise<GenericResponsePagination<IDivisionResponse> | void> => {
      try {
        return await fetchAndSetList<IDivisionResponse, DivisionFiltered>({
          apiCall: f => divisionService.getDivisionsByFilters(f),
          currentState: divisions,
          setState: setDivisions,
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
    [setDivisions, setError, divisions]
  );

  const getTopScoresByDivisionId = useCallback(
    async (id: GUID): Promise<DivisionTopScoreResponse[] | void> => {
      try {
        const res: AxiosResponse<DivisionTopScoreResponse[]> =
          await divisionService.getTopScoresByDivisionId(id);
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

  const deleteDivisionsById = useCallback(
    async (id: GUID): Promise<void> => {
      try {
        await divisionService.deleteDivisionsById(id);
        setDivision(null);
        setDivisions(prev => (prev ? prev.filter(e => e.id !== id) : null));
        setMessage(204, ['La división ha sido eliminada.']);
      } catch (error: unknown) {
        if (error instanceof AxiosError) {
          setError(error);
        } else {
          setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
        }
      }
    },
    [setDivision, setDivisions, setMessage, setError]
  );

  const container: IDivisionContextProps = useMemo(
    () => ({
      division,
      divisions,
      addDivision,
      generateFixtureByDivisionId,
      putDivisionById,
      getDivisionsByFilters,
      getDivisionsById,
      getTopScoresByDivisionId,
      deleteDivisionsById,
    }),
    [
      division,
      divisions,
      addDivision,
      generateFixtureByDivisionId,
      putDivisionById,
      getDivisionsByFilters,
      getDivisionsById,
      getTopScoresByDivisionId,
      deleteDivisionsById,
    ]
  );

  return (
    <DivisionContext.Provider value={container}>
      {children}
    </DivisionContext.Provider>
  );
};
