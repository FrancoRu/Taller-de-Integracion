import { InfoTeam } from '../team/info';
import { InfoTournament } from '../tournament/info';
import { InfoVenue } from '../venue/info';

export default function Home() {
  return (
    <>
      <InfoTournament />
      <InfoVenue />
      <InfoTeam />
    </>
  );
}
