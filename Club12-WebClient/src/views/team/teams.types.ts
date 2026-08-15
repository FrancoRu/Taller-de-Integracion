import { TeamFiltered } from '@/modules/team/type/team.d';

export type TeamsSearchFilters = Pick<
  TeamFiltered,
  'name' | 'threeLetterCode' | 'shirtColor'
>;

export type TeamFormState = {
  name: string;
  threeLetterCode: string;
  shirtColor: string;
  logo: File | null;
};
