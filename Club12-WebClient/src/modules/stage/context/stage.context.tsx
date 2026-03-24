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
} from '../type/stage.d';
import { useError } from '@/modules/error/hooks/error.hock';
import { upsertListById } from '@/modules/core/utils/synchronizeStates';
import { GenericResponsePagination, GUID } from '@/modules/core/types/types';
import { AxiosError, AxiosResponse } from 'axios';
import { stageService } from '../service/stage.service';
import { ERROR_MESSAGES } from '@/modules/core/constants/constants';

export const StageContext = createContext<IStageContextProps | undefined>(
  undefined
);

export const StageProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [stage, setStage] = useState<IStageResponse | null>(null);
  const [stages, setStages] = useState<IStageResponse[] | null>(null);

  const { setError, setMessage } = useError();
  const queryClient = useQueryClient();

  const handleUnknownError = useCallback(
    (error: unknown) => {
      if (error instanceof AxiosError) {
        setError(error);
        return;
      }

      setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
    },
    [setError]
  );

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
          queryClient.setQueryData(['stage', 'byId', res.data.id], res);
          await queryClient.invalidateQueries({ queryKey: ['stage', 'list'] });
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
        queryClient.setQueryData(['stage', 'byId', id], res);
        await queryClient.invalidateQueries({ queryKey: ['stage', 'list'] });
        return true;
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [putStageMutation, queryClient, handleUnknownError]
  );

  const getStageById = useCallback(
    async (id: GUID): Promise<IStageResponse | void> => {
      try {
        const existingStage: IStageResponse | undefined = stages?.find(
          e => e.id == id
        );

        if (existingStage) {
          setStage(existingStage);
          return existingStage;
        }

        const res: AxiosResponse<IStageResponse> = await queryClient.fetchQuery(
          {
            queryKey: ['stage', 'byId', id],
            queryFn: async () => await stageService.getStagesById(id),
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
          queryKey: ['stage', 'list', filter],
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
        queryClient.removeQueries({ queryKey: ['stage', 'byId', id] });
        await queryClient.invalidateQueries({ queryKey: ['stage', 'list'] });
        setMessage(204, ['La etapa ha sido eliminada.']);
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
          await queryClient.invalidateQueries({ queryKey: ['stage', 'list'] });
          return res.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [generateStagesMutation, queryClient, handleUnknownError]
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
    ]
  );

  return (
    <StageContext.Provider value={container}>{children}</StageContext.Provider>
  );
};
