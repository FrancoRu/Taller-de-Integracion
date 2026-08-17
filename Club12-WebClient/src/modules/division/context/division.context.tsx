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
import { GenericResponsePagination, GUID } from '@/modules/core/types/types';
import { useError } from '@/modules/error/hooks/error.hock';
import { useUnknownErrorHandler } from '@/modules/error/hooks/useUnknownErrorHandler';
import { divisionService } from '@/modules/division/service/division.service';
import {
  AddDivisionRequest,
  DivisionFiltered,
  IDivisionResponse,
  DivisionTopScoreResponse,
  IDivisionContextProps,
  IPutDivisionRequest,
} from '@/modules/division/type/division';
import { upsertListById } from '@/modules/core/utils/synchronizeStates';
import { divisionKeys } from '@/modules/division/queryKeys';
import { HttpStatus } from '@/modules/core/constants/httpStatus';

export const DivisionContext = createContext<IDivisionContextProps | undefined>(
  undefined
);

export const DivisionProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [division, setDivision] = useState<IDivisionResponse | null>(null);
  const [divisions, setDivisions] = useState<IDivisionResponse[] | null>(null);

  const { setMessage } = useError();
  const queryClient = useQueryClient();

  const handleUnknownError = useUnknownErrorHandler();

  const addDivisionMutation = useMutation({
    mutationFn: divisionService.addDivision,
  });

  const generateFixtureMutation = useMutation({
    mutationFn: divisionService.generateFixtureByDivisionId,
  });

  const putDivisionMutation = useMutation({
    mutationFn: ({
      id,
      divisionRequest,
    }: {
      id: GUID;
      divisionRequest: IPutDivisionRequest;
    }) => divisionService.putDivisionById(id, divisionRequest),
  });

  const deleteDivisionMutation = useMutation({
    mutationFn: divisionService.deleteDivisionsById,
  });

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
          await addDivisionMutation.mutateAsync(divisionRequest);
        if (res && res.data) {
          setDivision(res.data);
          queryClient.setQueryData(divisionKeys.byId(res.data.id), res);
          setMessage(res.status, ['Division creada exitosamente']);
          await queryClient.invalidateQueries({
            queryKey: divisionKeys.list(),
          });
          return res.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [addDivisionMutation, queryClient, setMessage, handleUnknownError]
  );

  const generateFixtureByDivisionId = useCallback(
    async (id: GUID): Promise<void> => {
      try {
        await generateFixtureMutation.mutateAsync(id);
        setMessage(HttpStatus.Ok, ['Fixture generado exitosamente']);
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [generateFixtureMutation, setMessage, handleUnknownError]
  );

  const putDivisionById = useCallback(
    async (
      id: GUID,
      divisionRequest: IPutDivisionRequest
    ): Promise<boolean | void> => {
      try {
        const res: AxiosResponse<IDivisionResponse> =
          await putDivisionMutation.mutateAsync({ id, divisionRequest });

        if (res && res.status === HttpStatus.NoContent) {
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
          await queryClient.invalidateQueries({
            queryKey: divisionKeys.list(),
          });
          return true;
        } else if (res && res.data) {
          setDivision(res.data);
          queryClient.setQueryData(divisionKeys.byId(id), res);
          setMessage(res.status, [
            'La información de la división fue actualizada correctamente',
          ]);
          await queryClient.invalidateQueries({
            queryKey: divisionKeys.list(),
          });
          return true;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [putDivisionMutation, setDivision, setMessage, queryClient, handleUnknownError]
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
          await queryClient.fetchQuery({
            queryKey: divisionKeys.byId(id),
            queryFn: async () => await divisionService.getDivisionsById(id),
          });

        if (res && res.data) {
          setDivision(res.data);
          return res.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [divisions, setDivision, queryClient, handleUnknownError]
  );

  const getDivisionsByFilters = useCallback(
    async (
      filter: DivisionFiltered
    ): Promise<GenericResponsePagination<IDivisionResponse> | void> => {
      try {
        const res = await queryClient.fetchQuery({
          queryKey: divisionKeys.list(filter),
          queryFn: async () =>
            await divisionService.getDivisionsByFilters(filter),
        });

        if (res?.data?.items) {
          setDivisions(res.data.items);
          return res.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [setDivisions, queryClient, handleUnknownError]
  );

  /**
   * Fails silently rather than through handleUnknownError: this enriches
   * the printable results sheet with a supplementary, non-critical
   * per-week scorer breakdown, and the backend endpoint it targets isn't
   * implemented yet. Surfacing an error toast to every visitor viewing
   * division standings would be worse than the sheet simply omitting the
   * breakdown, which PrintableResultsSheet already handles by defaulting
   * to an empty list.
   */
  const getTopScoresByDivisionId = useCallback(
    async (id: GUID): Promise<DivisionTopScoreResponse[] | void> => {
      try {
        const res: AxiosResponse<DivisionTopScoreResponse[]> =
          await queryClient.fetchQuery({
            queryKey: divisionKeys.topScorers(id),
            queryFn: async () =>
              await divisionService.getTopScoresByDivisionId(id),
          });

        return res.data;
      } catch {
        return undefined;
      }
    },
    [queryClient]
  );

  const deleteDivisionsById = useCallback(
    async (id: GUID): Promise<void> => {
      try {
        await deleteDivisionMutation.mutateAsync(id);
        setDivision(null);
        setDivisions(prev => (prev ? prev.filter(e => e.id !== id) : null));
        queryClient.removeQueries({ queryKey: divisionKeys.byId(id) });
        await queryClient.invalidateQueries({ queryKey: divisionKeys.list() });
        setMessage(HttpStatus.NoContent, ['La división ha sido eliminada.']);
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [deleteDivisionMutation, queryClient, setMessage, handleUnknownError]
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
