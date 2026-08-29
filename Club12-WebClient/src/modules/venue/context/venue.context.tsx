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
import { useError } from '@/modules/error/hooks/error.hock';
import { useUnknownErrorHandler } from '@/modules/error/hooks/useUnknownErrorHandler';
import { venueService } from '@/modules/venue/service/venue.service';
import {
  IAddVenueRequest,
  IVenueContextProps,
  IPutVenueRequest,
  IVenueResponse,
} from '@/modules/venue/type/venue';
import { GUID } from '@/modules/core/types/types';
import { upsertListById } from '@/modules/core/utils/synchronizeStates';
import { venueKeys } from '@/modules/venue/queryKeys';
import { HttpStatus } from '@/modules/core/constants/httpStatus';
import {
  extractProblemDetail,
  MutationResult,
} from '@/modules/core/utils/problemDetails';

export const VenueContext = createContext<IVenueContextProps | undefined>(
  undefined
);

export const VenueProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [venue, setVenue] = useState<IVenueResponse | null>(null);
  const [venues, setVenues] = useState<IVenueResponse[] | null>(null);

  const { setMessage } = useError();
  const queryClient = useQueryClient();

  const handleUnknownError = useUnknownErrorHandler();

  const addVenueMutation = useMutation({
    mutationFn: venueService.addVenue,
  });

  const putVenueMutation = useMutation({
    mutationFn: ({ id, venue }: { id: GUID; venue: IPutVenueRequest }) =>
      venueService.putVenueById(id, venue),
  });

  const deleteVenueMutation = useMutation({
    mutationFn: venueService.deleteVenueById,
  });

  useEffect(() => {
    if (!venue) return;
    setVenues(prev => upsertListById(prev, venue));
  }, [venue]);

  const addVenue = useCallback(
    async (venue: IAddVenueRequest): Promise<IVenueResponse | void> => {
      try {
        const res: AxiosResponse<IVenueResponse> =
          await addVenueMutation.mutateAsync(venue);

        if (res) {
          setVenue(res.data);
          queryClient.setQueryData(venueKeys.byId(res.data.id), res);
          await queryClient.invalidateQueries({ queryKey: venueKeys.list() });
          setMessage(res.status, ['La cancha fue creada exitosamente.']);
        }

        return res.data;
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [addVenueMutation, queryClient, setMessage, handleUnknownError]
  );

  const putVenueById = useCallback(
    async (
      id: GUID,
      venue: IPutVenueRequest
    ): Promise<IVenueResponse | void> => {
      try {
        const res: AxiosResponse<IVenueResponse> =
          await putVenueMutation.mutateAsync({ id, venue });

        if (res) {
          if (res.status === HttpStatus.NoContent) {
            const currentVenue =
              venues?.find(existingVenue => existingVenue.id === id) ?? null;
            const updatedVenue: IVenueResponse = {
              id,
              slug: currentVenue?.slug ?? '',
              name: venue.name ?? currentVenue?.name ?? '',
              address: venue.address ?? currentVenue?.address ?? '',
              photoUrl: venue.photoUrl ?? currentVenue?.photoUrl ?? '',
            };
            setVenue(updatedVenue);
            setVenues(prev => upsertListById(prev, updatedVenue));
            await queryClient.invalidateQueries({
              queryKey: venueKeys.list(),
            });
            setMessage(res.status, [
              'La información de la cancha fue actualizada correctamente',
            ]);
            return updatedVenue;
          } else if (res.data) {
            setVenue(res.data);
            setVenues(prev => upsertListById(prev, res.data));
            queryClient.setQueryData(venueKeys.byId(id), res);
            await queryClient.invalidateQueries({
              queryKey: venueKeys.list(),
            });
            setMessage(res.status, [
              'La información de la cancha fue actualizada correctamente',
            ]);
            return res.data;
          }
        }
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [putVenueMutation, queryClient, setMessage, venues, handleUnknownError]
  );

  const getAllVenues = useCallback(async (): Promise<
    IVenueResponse[] | void
  > => {
    try {
      const res: AxiosResponse<IVenueResponse[]> = await queryClient.fetchQuery(
        {
          queryKey: venueKeys.list(),
          queryFn: async () => await venueService.getAllVenues(),
        }
      );

      if (res) {
        setVenues(res.data);
      }
      return res.data;
    } catch (error: unknown) {
      handleUnknownError(error);
    }
  }, [queryClient, handleUnknownError]);

  const getVenueById = useCallback(
    async (idOrSlug: string): Promise<IVenueResponse | void> => {
      try {
        const existingVenue = venues?.find(
          e => e.id === idOrSlug || e.slug === idOrSlug
        );

        if (existingVenue) {
          setVenue(existingVenue);
          return existingVenue;
        }
        const res: AxiosResponse<IVenueResponse> = await queryClient.fetchQuery(
          {
            queryKey: venueKeys.byId(idOrSlug),
            queryFn: async () => await venueService.getVenueById(idOrSlug),
          }
        );

        if (res) {
          setVenue(res.data);
        }

        return res.data;
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [venues, queryClient, handleUnknownError]
  );

  /**
   * A venue delete can be blocked by a backend integrity rule (a venue still
   * referenced by matches returns a 409 with a Spanish message). The failure is
   * returned as a discriminated result so the caller can surface that exact
   * reason inline instead of swallowing it.
   */
  const deleteVenueById = useCallback(
    async (id: GUID): Promise<MutationResult> => {
      try {
        await deleteVenueMutation.mutateAsync(id);
        setVenue(null);
        setVenues(prev => (prev ? prev.filter(e => e.id !== id) : null));
        queryClient.removeQueries({ queryKey: venueKeys.byId(id) });
        await queryClient.invalidateQueries({ queryKey: venueKeys.list() });
        setMessage(HttpStatus.NoContent, ['La cancha ha sido eliminada.']);
        return { success: true };
      } catch (error: unknown) {
        return {
          success: false,
          errorMessage:
            extractProblemDetail(error) ??
            'No se pudo eliminar la cancha. Intentá nuevamente.',
        };
      }
    },
    [deleteVenueMutation, queryClient, setMessage]
  );

  const container: IVenueContextProps = useMemo(
    () => ({
      venue,
      venues,
      addVenue,
      getVenueById,
      getAllVenues,
      putVenueById,
      deleteVenueById,
    }),
    [
      venue,
      venues,
      addVenue,
      getVenueById,
      getAllVenues,
      putVenueById,
      deleteVenueById,
    ]
  );

  return (
    <VenueContext.Provider value={container}>{children}</VenueContext.Provider>
  );
};
