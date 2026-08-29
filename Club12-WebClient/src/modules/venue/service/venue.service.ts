import { AxiosResponse } from 'axios';
import routes from '@/modules/core/constants/routes';
import {
  sendDelete,
  sendGet,
  sendPost,
  sendPut,
} from '@/modules/core/utils/axiosUtils';
import {
  IAddVenueRequest,
  IPutVenueRequest,
  IVenueResponse,
} from '@/modules/venue/type/venue';
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
    if (venue.latitude !== undefined) {
      formData.append('Latitude', String(venue.latitude));
    }
    if (venue.longitude !== undefined) {
      formData.append('Longitude', String(venue.longitude));
    }
    return await sendPost(routes.venues, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
  },

  /**
   * Updates the photo for a specific venue. The backend binds
   * [FromForm] UpdateVenuePhotoRequest, which requires both a VenueId and the
   * ImageFile — send a proper multipart body.
   * @param {string} id - The ID of the venue to update.
   * @param {File} image - The new image file.
   * @returns {Promise<AxiosResponse<void>>} The server response.
   */
  putVenuePhotoById: async (
    id: GUID,
    image: File
  ): Promise<AxiosResponse<void>> => {
    const formData = new FormData();
    formData.append('VenueId', id);
    formData.append('ImageFile', image);
    return await sendPut(`${routes.venues}/${id}/photo`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
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
   * Retrieves a venue by its ID or its public slug.
   * @param {string} idOrSlug - The ID or slug of the venue to retrieve.
   * @returns {Promise<AxiosResponse<IVenueResponse>>} The server response containing the venue details.
   */
  getVenueById: async (
    idOrSlug: string
  ): Promise<AxiosResponse<IVenueResponse>> =>
    await sendGet(`${routes.venues}/${idOrSlug}`),

  /**
   * Deletes a venue by its ID.
   * @param {string} id - The ID of the venue to delete.
   * @returns {Promise<AxiosResponse<void>>} The server response.
   */
  deleteVenueById: async (id: GUID): Promise<AxiosResponse<void>> =>
    await sendDelete(`${routes.venues}/${id}`),
};
