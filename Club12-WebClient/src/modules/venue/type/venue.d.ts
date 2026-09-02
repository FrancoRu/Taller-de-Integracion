import { GUID } from '@/modules/core/types/types';
import { MutationResult } from '@/modules/core/utils/problemDetails';

/**
 * Context properties and methods for managing venue data in a React application.
 * These methods interact with the backend for creating, updating, fetching, and deleting venues.
 * @interface IVenueContextProps
 */
export interface IVenueContextProps {
  venue: IVenueResponse | null;
  venues: IVenueResponse[] | null;
  /**
   * Adds a new venue to the system.
   * @param venue The details of the venue to add.
   * @returns A promise that resolves with the response containing the newly added venue.
   */
  addVenue(venue: IAddVenueRequest): Promise<IVenueResponse | void>;

  /**
   * Updates an existing venue.
   * @param id The ID of the venue to put.
   * @param venue The updated venue details.
   * @returns A promise that resolves with the response containing the updated venue.
   */
  putVenueById(
    id: GUID,
    venue: IPutVenueRequest
  ): Promise<IVenueResponse | void>;

  /**
   * Uploads a new photo for an existing venue. The image is stored separately
   * from the venue's other fields (mirrors the team logo endpoint). Resolves
   * with the venue re-fetched from the backend so the caller gets the fresh
   * photo URL without a page reload.
   * @param id The ID of the venue whose photo to replace.
   * @param image The new image file.
   */
  putVenuePhotoById(id: GUID, image: File): Promise<IVenueResponse | void>;

  /**
   * Fetches all venues from the system.
   * @returns A promise that resolves with an array of venues.
   */
  getAllVenues(): Promise<IVenueResponse[] | void>;

  /**
   * Fetches a specific venue by its unique ID or its public slug.
   * @param idOrSlug The ID or slug of the venue to fetch.
   * @returns A promise that resolves with the venue data.
   */
  getVenueById(idOrSlug: string): Promise<IVenueResponse | void>;

  /**
   * Deletes a venue by its unique ID. Resolves with a discriminated result so
   * callers can surface a backend integrity block (a venue referenced by
   * matches is rejected with a 409 and a Spanish message).
   * @param id The ID of the venue to delete.
   */
  deleteVenueById(id: GUID): Promise<MutationResult>;
}

/**
 * The request body structure for adding a new venue.
 * @interface IAddVenueRequest
 */
export interface IAddVenueRequest {
  /**
   * The name of the venue.
   * @type {string}
   */
  name: string;

  /**
   * The address of the venue.
   * @type {string}
   */
  address: string;

  /**
   * Optional photo of the venue. A venue does not require an image.
   * @type {File}
   */
  imageFile?: File | null;

  /**
   * Optional geographic latitude of the venue.
   * @type {number}
   */
  latitude?: number;

  /**
   * Optional geographic longitude of the venue.
   * @type {number}
   */
  longitude?: number;
}

/**
 * The response structure when a venue is created or updated.
 * This extends from AddVenueRequest and includes an ID.
 * @interface IVenueResponse
 */
export interface IVenueResponse {
  /**
   * The unique identifier of the venue.
   * @type {string}
   */
  id: GUID;
  /**
   * The name of the venue.
   * @type {string}
   */
  name: string;

  /**
   * The unique, URL-friendly identifier used in public venue links.
   * @type {string}
   */
  slug: string;

  /**
   * The address of the venue.
   * @type {string}
   */
  address: string;

  photoUrl?: string;

  /**
   * Optional geographic latitude of the venue, for the public map link.
   * @type {number}
   */
  latitude?: number;

  /**
   * Optional geographic longitude of the venue, for the public map link.
   * @type {number}
   */
  longitude?: number;
}

/**
 * The request body structure for updating an existing venue.
 * It is the same as AddVenueRequest since only the venue details are updated.
 * @interface IPutVenueRequest
 */
export interface IPutVenueRequest {
  /**
   * The name of the venue.
   * @type {string}
   */
  name: string;

  /**
   * The address of the venue.
   * @type {string}
   */
  address: string;

  /**
   * The URL of the venue's photo.
   * @type {string}
   */
  photoUrl?: string;

  /**
   * Optional geographic latitude of the venue.
   * @type {number}
   */
  latitude?: number;

  /**
   * Optional geographic longitude of the venue.
   * @type {number}
   */
  longitude?: number;
}

export interface VenueDashboardProps {
  venues: IVenueResponse[];
}
