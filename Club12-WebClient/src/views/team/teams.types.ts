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
  /** Selected jersey kit template (e.g. `solid`, `stripes`). */
  jerseyStyle: string;
  logo: File | null;
};

/** The editable text/select fields of {@link TeamFormState}. */
export type TeamFormField =
  | 'name'
  | 'threeLetterCode'
  | 'shirtColor'
  | 'shirtSecondaryColor'
  | 'jerseyStyle';
