import { AxiosError, AxiosResponse } from 'axios';
import { createContext, ReactNode, useEffect, useState } from 'react';
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

export const VenueContext = createContext<IVenueContextProps | undefined>(
  undefined
);

export const VenueProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [venue, setVenue] = useState<IVenueResponse | null>(null);
  const [venues, SetVenues] = useState<IVenueResponse[] | null>(null);

  const { setError, setMessage } = useError();

  useEffect(() => {
    if (!venue) return;

    SetVenues(prev => upsertListById(prev, venue));
  }, [venue]);

  const addVenue = async (
    venue: IAddVenueRequest
  ): Promise<IVenueResponse | void> => {
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
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const putVenueById = async (
    id: GUID,
    venue: IPutVenueRequest
  ): Promise<IVenueResponse | void> => {
    try {
      const res: AxiosResponse<IVenueResponse> =
        await venueService.putVenueById(id, venue);

      if (res) {
        setVenue(res.data);
        setMessage(res.status, [
          'La informacion de la cancha fue actualizada correctamente',
        ]);
      }

      return res.data;
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const getAllVenues = async (): Promise<IVenueResponse[] | void> => {
    try {
      const res: AxiosResponse<IVenueResponse[]> =
        await venueService.getAllVenues();

      if (res) {
        SetVenues(res.data);
      }
      return res.data;
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const getVenueById = async (id: GUID): Promise<IVenueResponse | void> => {
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
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };
  const deleteVenueById = async (id: GUID): Promise<void> => {
    try {
      await venueService.deleteVenueById(id);
      setVenue(null);
      SetVenues(prev => (prev ? prev.filter(e => e.id !== id) : null));
      setMessage(204, ['La cancha ha sido eliminada.']);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const container: IVenueContextProps = {
    venue,
    venues,
    addVenue,
    getVenueById,
    getAllVenues,
    putVenueById,
    deleteVenueById,
  };
  return (
    <VenueContext.Provider value={container}>{children}</VenueContext.Provider>
  );
};
