export type VenueSearchFilters = {
  name?: string;
  address?: string;
};

export type VenueFormState = {
  name: string;
  address: string;
  latitude: string;
  longitude: string;
  /** A newly picked photo file to upload, if any. */
  photo: File | null;
  /** The venue's existing photo URL (shown as a preview while editing). */
  photoUrl: string;
};

/** The editable text fields of {@link VenueFormState}. */
export type VenueFormField = 'name' | 'address' | 'latitude' | 'longitude';
