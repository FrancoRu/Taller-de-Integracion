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
  IClubSummaryResponse,
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
  const [allClubs, setAllClubs] = useState<IClubSummaryResponse[]>([]);

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

  const linkClubParentMutation = useMutation({
    mutationFn: ({
      childClubId,
      parentClubId,
    }: {
      childClubId: GUID;
      parentClubId: GUID;
    }) => clubService.linkClubParent(childClubId, parentClubId),
  });

  const unlinkClubParentMutation = useMutation({
    mutationFn: (childClubId: GUID) => clubService.unlinkClubParent(childClubId),
  });

  const renameClubMutation = useMutation({
    mutationFn: ({ clubId, name }: { clubId: GUID; name: string }) =>
      clubService.renameClub(clubId, name),
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

  const getAllClubs = useCallback(async (): Promise<
    IClubSummaryResponse[] | void
  > => {
    try {
      const res: AxiosResponse<IClubSummaryResponse[]> =
        await queryClient.fetchQuery({
          queryKey: clubKeys.all(),
          queryFn: async () => await clubService.getAllClubs(),
        });

      if (res) {
        setAllClubs(res.data);
        return res.data;
      }
    } catch (error: unknown) {
      handleUnknownError(error);
    }
  }, [queryClient, handleUnknownError]);

  const linkClubParent = useCallback(
    async (
      childClubId: GUID,
      parentClubId: GUID
    ): Promise<IClubHistoryResponse | void> => {
      try {
        const res: AxiosResponse<IClubHistoryResponse> =
          await linkClubParentMutation.mutateAsync({
            childClubId,
            parentClubId,
          });
        if (res) {
          setClub(res.data);
          return res.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [linkClubParentMutation, handleUnknownError]
  );

  const unlinkClubParent = useCallback(
    async (childClubId: GUID): Promise<IClubHistoryResponse | void> => {
      try {
        const res: AxiosResponse<IClubHistoryResponse> =
          await unlinkClubParentMutation.mutateAsync(childClubId);
        if (res) {
          setClub(res.data);
          return res.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [unlinkClubParentMutation, handleUnknownError]
  );

  const renameClub = useCallback(
    async (clubId: GUID, name: string): Promise<IClubHistoryResponse | void> => {
      try {
        const res: AxiosResponse<IClubHistoryResponse> =
          await renameClubMutation.mutateAsync({ clubId, name });
        if (res) {
          setClub(res.data);
          return res.data;
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [renameClubMutation, handleUnknownError]
  );

  const container: IClubContextProps = useMemo(
    () => ({
      club,
      getClubHistory,
      copyRoster,
      allClubs,
      getAllClubs,
      linkClubParent,
      unlinkClubParent,
      renameClub,
    }),
    [
      club,
      getClubHistory,
      copyRoster,
      allClubs,
      getAllClubs,
      linkClubParent,
      unlinkClubParent,
      renameClub,
    ]
  );

  return (
    <ClubContext.Provider value={container}>{children}</ClubContext.Provider>
  );
};
