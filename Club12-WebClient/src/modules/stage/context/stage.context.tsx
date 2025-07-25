import { createContext, ReactNode, useEffect, useState } from 'react';
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

export const StageContext = createContext<IStageContextProps | undefined>(
  undefined
);

export const StageProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [stage, setStage] = useState<IStageResponse | null>(null);
  const [stages, setStages] = useState<IStageResponse[] | null>(null);

  const { setError, setMessage } = useError();

  useEffect(() => {
    if (!stage) return;

    setStages(prev => upsertListById(prev, stage));
  }, [stage]);

  const addStage = async (
    stage: IAddStageRequest
  ): Promise<IStageResponse | void> => {
    try {
      const res: AxiosResponse<IStageResponse> =
        await stageService.addStage(stage);
      if (res && res.data) {
        setStage(res.data);
        setMessage(res.status, ['Fase creada exitosamente']);
      }
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const putStageById = async (
    id: GUID,
    stageRequest: IPutStageRequest
  ): Promise<boolean | void> => {
    try {
      const res: AxiosResponse<IStageResponse> =
        await stageService.putStageById(id, stageRequest);

      if (res && res.status === 200) {
        setStage(res.data);
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

  const getStagesById = async (id: GUID): Promise<IStageResponse | void> => {
    try {
      const existinsstage: IStageResponse | undefined = stages?.find(
        e => e.id == id
      );

      if (existinsstage) {
        setStage(existinsstage);
        return existinsstage;
      }

      const res: AxiosResponse<IStageResponse> =
        await stageService.getStagesById(id);
      if (res && res.data) {
        setStage(res.data);
      }
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const getStagesByFilters = async (
    filter: StageFiltered
  ): Promise<GenericResponsePagination<IStageResponse> | void> => {
    try {
      const res: AxiosResponse<GenericResponsePagination<IStageResponse>> =
        await stageService.getStagesByFilters(filter);
      if (res) {
        setStages(res.data.items);
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

  const deleteStagesById = async (id: GUID): Promise<void> => {
    try {
      await stageService.deleteStagesById(id);
      setStage(null);
      setStages(prev => (prev ? prev.filter(e => e.id !== id) : null));
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const generateStagesAutomatically = async (
    id: GUID,
    quantityTeams: number
  ): Promise<boolean> => {
    try {
      console.log(id, quantityTeams);
      return true;
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
      return false; // en caso de error
    }
  };

  const container: IStageContextProps = {
    stage,
    stages,
    addStage,
    putStageById,
    getStagesByFilters,
    getStagesById,
    deleteStagesById,
    generateStagesAutomatically,
  };

  return (
    <StageContext.Provider value={container}>{children}</StageContext.Provider>
  );
};
