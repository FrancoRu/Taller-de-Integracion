import {
  createContext,
  ReactNode,
  useCallback,
  useEffect,
  useMemo,
  useState,
} from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  IAddStageRequest,
  IPutStageRequest,
  IStageContextProps,
  IStageResponse,
  StageFiltered,
} from '@/modules/stage/type/stage';
import { useError } from '@/modules/error/hooks/error.hock';
import { useUnknownErrorHandler } from '@/modules/error/hooks/useUnknownErrorHandler';
import { upsertListById } from '@/modules/core/utils/synchronizeStates';
import { GenericResponsePagination, GUID } from '@/modules/core/types/types';
import { AxiosResponse } from 'axios';
import { stageService } from '@/modules/stage/service/stage.service';
import { stageKeys } from '@/modules/stage/queryKeys';
import { HttpStatus } from '@/modules/core/constants/httpStatus';

export const StageContext = createContext<IStageContextProps | undefined>(
  undefined
);

export const StageProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [stage, setStage] = useState<IStageResponse | null>(null);
  const [stages, setStages] = useState<IStageResponse[] | null>(null);

  const { setMessage } = useError();
  const queryClient = useQueryClient();

  const handleUnknownError = useUnknownErrorHandler();

  const addStageMutation = useMutation({
    mutationFn: stageService.addStage,
  });

  const putStageMutation = useMutation({
    mutationFn: ({
      id,
      stageRequest,
    }: {
      id: GUID;
      stageRequest: IPutStageRequest;
    }) => stageService.putStageById(id, stageRequest),
  });

  const deleteStageMutation = useMutation({
    mutationFn: stageService.deleteStagesById,
  });

  const generateStagesMutation = useMutation({
    mutationFn: stageService.generateStages,
  });

  const assignTeamsMutation = useMutation({
    mutationFn: ({
      id,
      teamIds,
      auto,
    }: {
      id: GUID;
      teamIds: GUID[];
      auto: boolean;
    }) => stageService.assignTeamsToStage(id, teamIds, auto),
  });

  const unassignTeamsMutation = useMutation({
    mutationFn: ({ id, teamIds }: { id: GUID; teamIds: GUID[] }) =>
      stageService.unassignTeamsFromStage(id, teamIds),
  });

  const seedKnockoutStageMutation = useMutation({
    mutationFn: stageService.seedKnockoutStage,
  });

  useEffect(() => {
    if (!stage) return;

    setStages(prev => upsertListById(prev, stage));
  }, [stage]);

  const addStage = useCallback(
    async (stage: IAddStageRequest): Promise<IStageResponse | void> => {
      try {
        const res: AxiosResponse<IStageResponse> =
          await addStageMutation.mutateAsync(stage);
        if (res && res.data) {
          setStage(res.data);
          queryClient.setQueryData(stageKeys.byId(res.data.id), res);
          await queryClient.invalidateQueries({ queryKey: stageKeys.list() });
          setMessage(res.status, ['Fase creada exitosamente']);
        }
        return res.data;
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [addStageMutation, queryClient, setMessage, handleUnknownError]
  );

  const putStageById = useCallback(
    async (
      id: GUID,
      stageRequest: IPutStageRequest
    ): Promise<boolean | void> => {
      try {
        const res: AxiosResponse<IStageResponse> =
          await putStageMutation.mutateAsync({ id, stageRequest });
        setStage(res.data);
        queryClient.setQueryData(stageKeys.byId(id), res);
        await queryClient.invalidateQueries({ queryKey: stageKeys.list() });
        return true;
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [putStageMutation, queryClient, handleUnknownError]
  );

  const getStageById = useCallback(
    async (idOrSlug: string): Promise<IStageResponse | void> => {
      try {
        const existingStage: IStageResponse | undefined = stages?.find(
          e => e.id === idOrSlug || e.slug === idOrSlug
        );

        if (existingStage) {
          setStage(existingStage);
          return existingStage;
        }

        const res: AxiosResponse<IStageResponse> = await queryClient.fetchQuery(
          {
            queryKey: stageKeys.byId(idOrSlug),
            queryFn: async () => await stageService.getStagesById(idOrSlug),
          }
        );

        if (res && res.data) {
          setStage(res.data);
          return res.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [stages, queryClient, handleUnknownError]
  );

  const getStagesByFilters = useCallback(
    async (
      filter: StageFiltered
    ): Promise<GenericResponsePagination<IStageResponse> | void> => {
      try {
        const res = await queryClient.fetchQuery({
          queryKey: stageKeys.list(filter),
          queryFn: async () => await stageService.getStagesByFilters(filter),
        });

        if (res?.data?.items) {
          setStages(res.data.items);
          return res.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [queryClient, handleUnknownError]
  );

  const deleteStagesById = useCallback(
    async (id: GUID): Promise<void> => {
      try {
        await deleteStageMutation.mutateAsync(id);
        setStage(null);
        setStages(prev => (prev ? prev.filter(e => e.id !== id) : null));
        queryClient.removeQueries({ queryKey: stageKeys.byId(id) });
        await queryClient.invalidateQueries({ queryKey: stageKeys.list() });
        setMessage(HttpStatus.NoContent, ['La etapa ha sido eliminada.']);
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [deleteStageMutation, queryClient, setMessage, handleUnknownError]
  );

  const generateStagesAutomatically = useCallback(
    async (id: GUID): Promise<IStageResponse[] | void> => {
      try {
        const res: AxiosResponse<IStageResponse[]> =
          await generateStagesMutation.mutateAsync(id);

        if (res && res.data) {
          setStages(res.data);
          await queryClient.invalidateQueries({ queryKey: stageKeys.list() });
          return res.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [generateStagesMutation, queryClient, handleUnknownError]
  );

  const assignTeamsToStage = useCallback(
    async (id: GUID, teamIds: GUID[], auto = false): Promise<boolean | void> => {
      try {
        const res = await assignTeamsMutation.mutateAsync({ id, teamIds, auto });
        if (res) {
          await queryClient.invalidateQueries({ queryKey: stageKeys.list() });
          return true;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [assignTeamsMutation, queryClient, handleUnknownError]
  );

  const unassignTeamsFromStage = useCallback(
    async (id: GUID, teamIds: GUID[]): Promise<boolean | void> => {
      try {
        const res = await unassignTeamsMutation.mutateAsync({ id, teamIds });
        if (res) {
          await queryClient.invalidateQueries({ queryKey: stageKeys.list() });
          return true;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [unassignTeamsMutation, queryClient, handleUnknownError]
  );

  const seedKnockoutStage = useCallback(
    async (id: GUID): Promise<boolean | void> => {
      try {
        const res = await seedKnockoutStageMutation.mutateAsync(id);
        if (res) {
          await queryClient.invalidateQueries({ queryKey: stageKeys.list() });
          return true;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [seedKnockoutStageMutation, queryClient, handleUnknownError]
  );

  const container: IStageContextProps = useMemo(
    () => ({
      stage,
      stages,
      addStage,
      putStageById,
      getStagesByFilters,
      getStageById,
      deleteStagesById,
      generateStagesAutomatically,
      assignTeamsToStage,
      unassignTeamsFromStage,
      seedKnockoutStage,
    }),
    [
      stage,
      stages,
      addStage,
      putStageById,
      getStagesByFilters,
      getStageById,
      deleteStagesById,
      generateStagesAutomatically,
      assignTeamsToStage,
      unassignTeamsFromStage,
      seedKnockoutStage,
    ]
  );

  return (
    <StageContext.Provider value={container}>{children}</StageContext.Provider>
  );
};
