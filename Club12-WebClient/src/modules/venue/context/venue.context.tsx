import { AxiosError, AxiosResponse } from 'axios';
import {
  createContext,
  ReactNode,
  useEffect,
  useState,
  useCallback,
  useMemo,
} from 'react';
import { useError } from '../../error/hooks/error.hock';
import { venueService } from '../service/venue.service';
import {
  IAddVenueRequest,
  IVenueContextProps,
  IPutVenueRequest,
  IVenueResponse,
} from '../type/venue';
import { GUID } from '@/modules/core/types/types';
import { upsertListById } from '@/modules/core/utils/synchronizeStates';
import { ERROR_MESSAGES } from '@/modules/core/constants/constants';

export const VenueContext = createContext<IVenueContextProps | undefined>(
  undefined
);

export const VenueProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [venue, setVenue] = useState<IVenueResponse | null>(null);
  const [venues, setVenues] = useState<IVenueResponse[] | null>(null);

  const { setError, setMessage } = useError();

  useEffect(() => {
    if (!venue) return;
    // Usamos el nuevo nombre setVenues
    setVenues(prev => upsertListById(prev, venue));
  }, [venue]);

  const addVenue = useCallback(
    async (venue: IAddVenueRequest): Promise<IVenueResponse | void> => {
      try {
        const res: AxiosResponse<IVenueResponse> =
          await venueService.addVenue(venue);

        if (res) {
          setVenue(res.data);
          setMessage(res.status, ['La cancha fue creada exitosamente.']);
        }

        return res.data;
      } catch (error: unknown) {
        if (error instanceof AxiosError) {
          setError(error);
        } else {
          setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
        }
      }
    },
    [setVenue, setMessage, setError]
  );

  const putVenueById = useCallback(
    async (
      id: GUID,
      venue: IPutVenueRequest
    ): Promise<IVenueResponse | void> => {
      try {
        const res: AxiosResponse<IVenueResponse> =
          await venueService.putVenueById(id, venue);

        if (res) {
          setVenue(res.data);
          setMessage(res.status, [
            'La información de la cancha fue actualizada correctamente',
          ]);
        }

        return res.data;
      } catch (error: unknown) {
        if (error instanceof AxiosError) {
          setError(error);
        } else {
          setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
        }
      }
    },
    [setVenue, setMessage, setError]
  );

  const getAllVenues = useCallback(
    async (): Promise<IVenueResponse[] | void> => {
      try {
        const res: AxiosResponse<IVenueResponse[]> =
          await venueService.getAllVenues();

        if (res) {
          setVenues(res.data);
        }
        return res.data;
      } catch (error: unknown) {
        if (error instanceof AxiosError) {
          setError(error);
        } else {
          setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
        }
      }
    },
    [setVenues, setError] // Dependencias: setVenues (el setter), setError
  );

  const getVenueById = useCallback(
    async (id: GUID): Promise<IVenueResponse | void> => {
      try {
        const existingVenue = venues?.find(e => e.id == id);

        if (existingVenue) {
          setVenue(existingVenue);
          return existingVenue;
        }
        const res: AxiosResponse<IVenueResponse> =
          await venueService.getVenueById(id);

        if (res) {
          setVenue(res.data);
        }

        return res.data;
      } catch (error: unknown) {
        if (error instanceof AxiosError) {
          setError(error);
        } else {
          setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
        }
      }
    },
    [venues, setVenue, setError] // Dependencias: venues (para el find), setVenue, setError
  );

  const deleteVenueById = useCallback(
    async (id: GUID): Promise<void> => {
      try {
        await venueService.deleteVenueById(id);
        setVenue(null);
        setVenues(prev => (prev ? prev.filter(e => e.id !== id) : null)); // Usamos el nuevo nombre setVenues
        setMessage(204, ['La cancha ha sido eliminada.']);
      } catch (error: unknown) {
        if (error instanceof AxiosError) {
          setError(error);
        } else {
          setError(new AxiosError(ERROR_MESSAGES.GENERIC_ERROR));
        }
      }
    },
    [setVenue, setVenues, setMessage, setError] // Dependencias: setVenue, setVenues, setMessage, setError
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
