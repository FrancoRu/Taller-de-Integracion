import { TeamFiltered } from '@/modules/team/type/team.d';

export type TeamsSearchFilters = Pick<
  TeamFiltered,
  'name' | 'threeLetterCode' | 'shirtColor'
>;

export type TeamFormState = {
  name: string;
  threeLetterCode: string;
  shirtColor: string;
  /** Secondary kit color; empty string means "derive automatically". */
  shirtSecondaryColor: string;
  /** Third kit color; empty string means "unset" (only some templates use it). */
  shirtTertiaryColor: string;
  /** Selected jersey kit template (e.g. `solid`, `stripes`). */
  jerseyStyle: string;
  /** A newly picked logo file to upload, if any. */
  logo: File | null;
  /** The team's existing logo URL (shown as a preview while editing). */
  logoUrl: string;
};

/** The editable text/select fields of {@link TeamFormState}. */
export type TeamFormField =
  | 'name'
  | 'threeLetterCode'
  | 'shirtColor'
  | 'shirtSecondaryColor'
  | 'shirtTertiaryColor'
  | 'jerseyStyle';
