import { AxiosResponse } from 'axios';
import routes from '../../core/constants/routes';
import {
  sendDelete,
  sendGet,
  sendPost,
  sendPut,
} from '../../core/utils/axiosUtils';
import { AddVenueRequest, PutVenueRequest, VenueResponse } from '../type/venue';

/**
 * Service for managing venues.
 */
export const venueService = {
  /**
   * Adds a new venue.
   * @param {AddVenueRequest} venue - The venue details to add.
   * @returns {Promise<AxiosResponse<VenueResponse>>} The server response.
   */
  addVenue: async (
    venue: AddVenueRequest
  ): Promise<AxiosResponse<VenueResponse>> =>
    await sendPost(routes.venues, venue),

  /**
   * Updates an existing venue.
   * @param {string} id - The ID of the venue to update.
   * @param {PutVenueRequest} venue - The updated venue details.
   * @returns {Promise<AxiosResponse<VenueResponse>>} The server response.
   */
  putVenueById: async (
    id: string,
    venue: PutVenueRequest
  ): Promise<AxiosResponse<VenueResponse>> =>
    await sendPut(`${routes.venues}/${id}`, venue),

  /**
   * Retrieves all venues.
   * @returns {Promise<AxiosResponse<VenueResponse[]>>} The server response containing the list of venues.
   */
  getAllVenues: async (): Promise<AxiosResponse<VenueResponse[]>> =>
    await sendGet(routes.venues),

  /**
   * Retrieves a venue by its ID.
   * @param {string} id - The ID of the venue to retrieve.
   * @returns {Promise<AxiosResponse<VenueResponse>>} The server response containing the venue details.
   */
  getVenueById: async (id: string): Promise<AxiosResponse<VenueResponse>> =>
    await sendGet(`${routes.venues}/${id}`),

  /**
   * Deletes a venue by its ID.
   * @param {string} id - The ID of the venue to delete.
   * @returns {Promise<AxiosResponse<void>>} The server response.
   */
  deleteVenueById: async (id: string): Promise<AxiosResponse<void>> =>
    await sendDelete(`${routes.venues}/${id}`),
};
