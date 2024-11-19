/**
 * Context properties and methods for managing venue data in a React application.
 * These methods interact with the backend for creating, updating, fetching, and deleting venues.
 * @interface IVenueContextProps
 */
export interface IVenueContextProps {
  /**
   * Adds a new venue to the system.
   * @param venue The details of the venue to add.
   * @returns A promise that resolves with the response containing the newly added venue.
   */
  addVenue(venue: AddVenueRequest): Promise<VenueResponse | void>;

  /**
   * Updates an existing venue.
   * @param id The ID of the venue to put.
   * @param venue The updated venue details.
   * @returns A promise that resolves with the response containing the updated venue.
   */
  putVenueById(
    id: string,
    venue: PutVenueRequest
  ): Promise<VenueResponse | void>;

  /**
   * Fetches all venues from the system.
   * @returns A promise that resolves with an array of venues.
   */
  getAllVenues(): Promise<VenueResponse[] | void>;

  /**
   * Fetches a specific venue by its unique ID.
   * @param id The ID of the venue to fetch.
   * @returns A promise that resolves with the venue data.
   */
  getVenueById(id: string): Promise<VenueResponse | void>;

  /**
   * Deletes a venue by its unique ID.
   * @param id The ID of the venue to delete.
   * @returns A promise that resolves when the venue is successfully deleted.
   */
  deleteVenueById(id: string): Promise<void>;
}

/**
 * The request body structure for adding a new venue.
 * @interface AddVenueRequest
 */
export interface AddVenueRequest {
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
  photoUrl: string;
}

/**
 * The response structure when a venue is created or updated.
 * This extends from AddVenueRequest and includes an ID.
 * @interface VenueResponse
 */
export interface VenueResponse extends AddVenueRequest {
  /**
   * The unique identifier of the venue.
   * @type {string}
   */
  id: string;
}

/**
 * The request body structure for updating an existing venue.
 * It is the same as AddVenueRequest since only the venue details are updated.
 * @interface PutVenueRequest
 */
export interface PutVenueRequest {
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
