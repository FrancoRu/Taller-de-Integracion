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
   * Fetches all venues from the system.
   * @returns A promise that resolves with an array of venues.
   */
  getAllVenues(): Promise<IVenueResponse[] | void>;

  /**
   * Fetches a specific venue by its unique ID.
   * @param id The ID of the venue to fetch.
   * @returns A promise that resolves with the venue data.
   */
  getVenueById(id: GUID): Promise<IVenueResponse | void>;

  /**
   * Deletes a venue by its unique ID.
   * @param id The ID of the venue to delete.
   * @returns A promise that resolves when the venue is successfully deleted.
   */
  deleteVenueById(id: GUID): Promise<void>;
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
   * The image file of the venue's photo.
   * @type {File}
   */
  imageFile: File;
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
   * The address of the venue.
   * @type {string}
   */
  address: string;

  photoUrl: string;
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
  name?: string;

  /**
   * The address of the venue.
   * @type {string}
   */
  address?: string;

  /**
   * The URL of the venue's photo.
   * @type {string}
   */
  photoUrl?: string;
}
