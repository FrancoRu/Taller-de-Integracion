import { AxiosResponse } from 'axios';
import React, {
  createContext,
  ReactNode,
  useCallback,
  useMemo,
} from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { GUID } from '@/modules/core/types/types';
import { useUnknownErrorHandler } from '@/modules/error/hooks/useUnknownErrorHandler';
import { medicalRecordService } from '@/modules/medicalRecord/service/medicalRecord.service';
import { medicalRecordKeys } from '@/modules/medicalRecord/queryKeys';
import {
  IMedicalRecordContextProps,
  IMedicalRecordResponse,
  IReviewMedicalRecordRequest,
  IUploadMedicalRecordRequest,
} from '@/modules/medicalRecord/type/medicalRecord.d';

export const MedicalRecordContext = createContext<
  IMedicalRecordContextProps | undefined
>(undefined);

export const MedicalRecordProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const queryClient = useQueryClient();
  const handleUnknownError = useUnknownErrorHandler();

  const uploadMutation = useMutation({
    mutationFn: medicalRecordService.uploadMedicalRecord,
  });

  const reviewMutation = useMutation({
    mutationFn: medicalRecordService.reviewMedicalRecord,
  });

  const uploadMedicalRecord = useCallback(
    async (
      request: IUploadMedicalRecordRequest
    ): Promise<IMedicalRecordResponse | void> => {
      try {
        const res: AxiosResponse<IMedicalRecordResponse> =
          await uploadMutation.mutateAsync(request);
        if (res) {
          queryClient.setQueryData(
            medicalRecordKeys.byRegistration(
              request.playerId,
              request.teamId,
              request.tournamentId
            ),
            res
          );
          return res.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [uploadMutation, queryClient, handleUnknownError]
  );

  const reviewMedicalRecord = useCallback(
    async (
      request: IReviewMedicalRecordRequest
    ): Promise<IMedicalRecordResponse | void> => {
      try {
        const res: AxiosResponse<IMedicalRecordResponse> =
          await reviewMutation.mutateAsync(request);
        if (res) {
          queryClient.setQueryData(
            medicalRecordKeys.byRegistration(
              request.playerId,
              request.teamId,
              request.tournamentId
            ),
            res
          );
          return res.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [reviewMutation, queryClient, handleUnknownError]
  );

  const getMedicalRecord = useCallback(
    async (
      playerId: GUID,
      teamId: GUID,
      tournamentId: GUID
    ): Promise<IMedicalRecordResponse | void> => {
      try {
        const res: AxiosResponse<IMedicalRecordResponse> =
          await queryClient.fetchQuery({
            queryKey: medicalRecordKeys.byRegistration(
              playerId,
              teamId,
              tournamentId
            ),
            queryFn: async () =>
              await medicalRecordService.getMedicalRecord(
                playerId,
                teamId,
                tournamentId
              ),
          });

        if (res) {
          return res.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [queryClient, handleUnknownError]
  );

  const container: IMedicalRecordContextProps = useMemo(
    () => ({
      uploadMedicalRecord,
      reviewMedicalRecord,
      getMedicalRecord,
    }),
    [uploadMedicalRecord, reviewMedicalRecord, getMedicalRecord]
  );

  return (
    <MedicalRecordContext.Provider value={container}>
      {children}
    </MedicalRecordContext.Provider>
  );
};
