import { AxiosError } from 'axios';
import { createContext, ReactNode } from 'react';
import { useError } from '../../error/hooks/error.hock';
import { venueService } from '../service/venue.service';
import {
  AddVenueRequest,
  IVenueContextProps,
  PutVenueRequest,
  VenueResponse,
} from '../type/venue';

export const VenueContext = createContext<IVenueContextProps | undefined>(
  undefined
);

export const VenueProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const { setError } = useError();

  const addVenue = async (
    venue: AddVenueRequest
  ): Promise<VenueResponse | void> => {
    try {
      await venueService.addVenue(venue);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const putVenueById = async (
    id: string,
    venue: PutVenueRequest
  ): Promise<VenueResponse | void> => {
    try {
      await venueService.putVenueById(id, venue);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const getAllVenues = async (): Promise<VenueResponse[] | void> => {
    try {
      await venueService.getAllVenues();
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const getVenueById = async (id: string): Promise<VenueResponse | void> => {
    try {
      await venueService.getVenueById(id);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };
  const deleteVenueById = async (id: string): Promise<void> => {
    try {
      await venueService.deleteVenueById(id);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError('An unknown error occurred'));
      }
    }
  };

  const container: IVenueContextProps = {
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
