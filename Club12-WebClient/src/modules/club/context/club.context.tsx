import { AxiosResponse } from 'axios';
import {
  createContext,
  ReactNode,
  useCallback,
  useMemo,
  useState,
} from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { GUID } from '@/modules/core/types/types';
import { useUnknownErrorHandler } from '@/modules/error/hooks/useUnknownErrorHandler';
import { clubService } from '@/modules/club/service/club.service';
import { clubKeys } from '@/modules/club/queryKeys';
import {
  IClubContextProps,
  IClubHistoryResponse,
  IRosterCopyRequest,
  IRosterCopyResult,
} from '@/modules/club/type/club.d';

export const ClubContext = createContext<IClubContextProps | undefined>(
  undefined
);

export const ClubProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [club, setClub] = useState<IClubHistoryResponse | null>(null);

  const queryClient = useQueryClient();
  const handleUnknownError = useUnknownErrorHandler();

  const copyRosterMutation = useMutation({
    mutationFn: ({
      targetTeamId,
      request,
    }: {
      targetTeamId: GUID;
      request: IRosterCopyRequest;
    }) => clubService.copyRoster(targetTeamId, request),
  });

  const getClubHistory = useCallback(
    async (idOrSlug: string): Promise<IClubHistoryResponse | void> => {
      try {
        const res: AxiosResponse<IClubHistoryResponse> =
          await queryClient.fetchQuery({
            queryKey: clubKeys.history(idOrSlug),
            queryFn: async () => await clubService.getClubHistory(idOrSlug),
          });

        if (res) {
          setClub(res.data);
          return res.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [queryClient, handleUnknownError]
  );

  const copyRoster = useCallback(
    async (
      targetTeamId: GUID,
      request: IRosterCopyRequest
    ): Promise<IRosterCopyResult | void> => {
      try {
        const res: AxiosResponse<IRosterCopyResult> =
          await copyRosterMutation.mutateAsync({ targetTeamId, request });
        return res?.data;
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [copyRosterMutation, handleUnknownError]
  );

  const container: IClubContextProps = useMemo(
    () => ({
      club,
      getClubHistory,
      copyRoster,
    }),
    [club, getClubHistory, copyRoster]
  );

  return (
    <ClubContext.Provider value={container}>{children}</ClubContext.Provider>
  );
};
