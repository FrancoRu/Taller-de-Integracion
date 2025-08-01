import { AxiosResponse } from 'axios';
import routes from '../../core/constants/routes';
import {
  sendDelete,
  sendGet,
  sendPost,
  sendPut,
} from '../../core/utils/axiosUtils';
import {
  IAddVenueRequest,
  IPutVenueRequest,
  IVenueResponse,
} from '../type/venue';
import { GUID } from '@/modules/core/types/types';

/**
 * Service for managing venues.
 */
export const venueService = {
  /**
   * Adds a new venue.
   * @param {IAddVenueRequest} venue - The venue details to add.
   * @returns {Promise<AxiosResponse<IVenueResponse>>} The server response.
   */
  addVenue: async (
    venue: IAddVenueRequest
  ): Promise<AxiosResponse<IVenueResponse>> => {
    const formData = new FormData();
    formData.append('Name', venue.name);
    formData.append('Address', venue.address);
    formData.append('ImageFile', venue.imageFile);
    return await sendPost(routes.venues, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
  },

  /**
   * Updates an existing venue.
   * @param {string} id - The ID of the venue to update.
   * @param {IPutVenueRequest} venue - The updated venue details.
   * @returns {Promise<AxiosResponse<IVenueResponse>>} The server response.
   */
  putVenueById: async (
    id: GUID,
    venue: IPutVenueRequest
  ): Promise<AxiosResponse<IVenueResponse>> =>
    await sendPut(`${routes.venues}/${id}`, venue),

  /**
   * Retrieves all venues.
   * @returns {Promise<AxiosResponse<IVenueResponse[]>>} The server response containing the list of venues.
   */
  getAllVenues: async (): Promise<AxiosResponse<IVenueResponse[]>> =>
    await sendGet(routes.venues),

  /**
   * Retrieves a venue by its ID.
   * @param {string} id - The ID of the venue to retrieve.
   * @returns {Promise<AxiosResponse<IVenueResponse>>} The server response containing the venue details.
   */
  getVenueById: async (id: GUID): Promise<AxiosResponse<IVenueResponse>> =>
    await sendGet(`${routes.venues}/${id}`),

  /**
   * Deletes a venue by its ID.
   * @param {string} id - The ID of the venue to delete.
   * @returns {Promise<AxiosResponse<void>>} The server response.
   */
  deleteVenueById: async (id: GUID): Promise<AxiosResponse<void>> =>
    await sendDelete(`${routes.venues}/${id}`),
};
