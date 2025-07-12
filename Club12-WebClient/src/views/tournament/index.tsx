import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import {
  ITournamentContextProps,
  ITournamentViewProps,
  TournamentResponse,
} from '@/modules/tournament/type/tournament';
import React, { useEffect, useState } from 'react';
import { Fixture } from './common/fixture';
import { Positions } from './common/positions';
import { TopScores } from './common/topScores';
import { DivisionProvider } from '@/modules/division/context/division.context';
import { DivisionResponse } from '@/modules/division/type/division';
import { TypeMatch } from '@/modules/match/type/match.d';

export const Tournament: React.FC<ITournamentViewProps> = ({ id }) => {
  const context: ITournamentContextProps = useTournament();

  const [tournament, setTournament] = useState<TournamentResponse>();

  useEffect(() => {
    (async () => {
      const res: TournamentResponse | void =
        await context.getTournamentById(id);

      if (res) {
        setTournament(res);
      }
    })();
    setTournament({ name: 'test', id: id, description: 'descriptionTest' });
  }, [tournament]);

  return (
    <>
      {!tournament ? (
        <h1>Tournament not found</h1>
      ) : (
        <>
          <h1> {tournament.name}</h1>
          <DivisionProvider>
            <Fixture divisions={dummyDivisions} />
          </DivisionProvider>
          <Positions />
          <TopScores />
        </>
      )}
    </>
  );
};

export const dummyDivisions: DivisionResponse[] = [
  {
    name: 'Fecha 1',
    id: 'division-123',
    isFinished: false,
    positions: [],
    tournamentId: 'tournament-456',
    matchesByWeek: [
      {
        id: 'match-1',
        matchDate: '2025-07-01T15:00:00',
        type: TypeMatch.Regular,
        matchWeek: 1,
        homeTeamId: 'team-1',
        homeTeamName: 'Equipo A',
        homeTeamLogoUrl:
          'https://pub-a1c2f3559aad439486e2d0fe7766801c.r2.dev/MAGIOS.png',
        visitorTeamId: 'team-2',
        visitorTeamName: 'Equipo B',
        visitorTeamLogoUrl:
          'https://pub-a1c2f3559aad439486e2d0fe7766801c.r2.dev/TUKI%20CLUB.png',
        homeScore: 2,
        visitorScore: 1,
        isFinished: true,
        winningTeamName: 'Equipo A',
        venue: {
          id: 'venue-1',
          name: 'Estadio Uno',
          address: '',
          photoUrl: '',
        },
      },
      {
        id: 'match-2',
        matchDate: '2025-07-02T17:00:00',
        type: TypeMatch.Regular,
        matchWeek: 1,
        homeTeamId: 'team-3',
        homeTeamName: 'Equipo C',
        homeTeamLogoUrl:
          'https://pub-a1c2f3559aad439486e2d0fe7766801c.r2.dev/cachuchans.png',
        visitorTeamId: 'team-4',
        visitorTeamName: 'Equipo D',
        visitorTeamLogoUrl:
          'https://pub-a1c2f3559aad439486e2d0fe7766801c.r2.dev/eep.png',
        homeScore: 0,
        visitorScore: 3,
        isFinished: true,
        winningTeamName: 'Equipo D',
        venue: {
          id: 'venue-2',
          name: 'La Bombonera',
          address: '',
          photoUrl: '',
        },
      },
      {
        id: 'match-3',
        matchDate: '2025-07-03T20:00:00',
        type: TypeMatch.Regular,
        matchWeek: 2,
        homeTeamId: 'team-5',
        homeTeamName: 'Equipo E',
        homeTeamLogoUrl:
          '	https://pub-a1c2f3559aad439486e2d0fe7766801c.r2.dev/NATALIA%20NATALIA.png',
        visitorTeamId: 'team-6',
        visitorTeamName: 'Equipo F',
        visitorTeamLogoUrl:
          'https://pub-a1c2f3559aad439486e2d0fe7766801c.r2.dev/TANOS.png',
        homeScore: 1,
        visitorScore: 1,
        isFinished: true,
        winningTeamName: '',
        venue: {
          id: 'venue-3',
          name: 'Estadio Racing',
          address: '',
          photoUrl: '',
        },
      },
      {
        id: 'match-4',
        matchDate: '2025-07-04T18:00:00',
        type: TypeMatch.Regular,
        matchWeek: 2,
        homeTeamId: 'team-7',
        homeTeamName: 'Equipo G',
        homeTeamLogoUrl:
          '	https://pub-a1c2f3559aad439486e2d0fe7766801c.r2.dev/LBY.png',
        visitorTeamId: 'team-8',
        visitorTeamName: 'Equipo H',
        visitorTeamLogoUrl:
          '	https://pub-a1c2f3559aad439486e2d0fe7766801c.r2.dev/BLUE%20LABEL.png',
        homeScore: 0,
        visitorScore: 2,
        isFinished: true,
        winningTeamName: 'Equipo H',
        venue: {
          id: 'venue-4',
          name: 'Estadio Tigre',
          address: '',
          photoUrl: '',
        },
      },
    ],
  },
  {
    name: 'Fecha 2',
    id: 'division-456',
    isFinished: false,
    positions: [],
    tournamentId: 'tournament-456',
    matchesByWeek: [
      {
        id: 'match-5',
        matchDate: '2025-07-05T19:30:00',
        type: TypeMatch.Regular,
        matchWeek: 3,
        homeTeamId: 'team-1',
        homeTeamName: 'Equipo A',
        homeTeamLogoUrl:
          'https://pub-a1c2f3559aad439486e2d0fe7766801c.r2.dev/MAGIOS.png',
        visitorTeamId: 'team-4',
        visitorTeamName: 'Equipo D',
        visitorTeamLogoUrl:
          'https://pub-a1c2f3559aad439486e2d0fe7766801c.r2.dev/eep.png',
        homeScore: 1,
        visitorScore: 0,
        isFinished: false,
        winningTeamName: '',
        venue: {
          id: 'venue-5',
          name: 'Estadio Final',
          address: '',
          photoUrl: '',
        },
      },
      {
        id: 'match-6',
        matchDate: '2025-07-06T20:00:00',
        type: TypeMatch.Regular,
        matchWeek: 3,
        homeTeamId: 'team-2',
        homeTeamName: 'Equipo B',
        homeTeamLogoUrl:
          'https://pub-a1c2f3559aad439486e2d0fe7766801c.r2.dev/TUKI%20CLUB.png',
        visitorTeamId: 'team-5',
        visitorTeamName: 'Equipo E',
        visitorTeamLogoUrl:
          '	https://pub-a1c2f3559aad439486e2d0fe7766801c.r2.dev/NATALIA%20NATALIA.png',
        homeScore: 2,
        visitorScore: 3,
        isFinished: false,
        winningTeamName: '',
        venue: {
          id: 'venue-6',
          name: 'Estadio Parcial',
          address: '',
          photoUrl: '',
        },
      },
      {
        id: 'match-7',
        matchDate: '2025-07-07T18:00:00',
        type: TypeMatch.Regular,
        matchWeek: 4,
        homeTeamId: 'team-3',
        homeTeamName: 'Equipo C',
        homeTeamLogoUrl:
          'https://pub-a1c2f3559aad439486e2d0fe7766801c.r2.dev/cachuchans.png',
        visitorTeamId: 'team-6',
        visitorTeamName: 'Equipo F',
        visitorTeamLogoUrl:
          'https://pub-a1c2f3559aad439486e2d0fe7766801c.r2.dev/TANOS.png',
        homeScore: 0,
        visitorScore: 1,
        isFinished: false,
        winningTeamName: '',
        venue: {
          id: 'venue-7',
          name: 'Estadio Semi',
          address: '',
          photoUrl: '',
        },
      },
      {
        id: 'match-8',
        matchDate: '2025-07-08T21:00:00',
        type: TypeMatch.Regular,
        matchWeek: 4,
        homeTeamId: 'team-7',
        homeTeamName: 'Equipo G',
        homeTeamLogoUrl:
          '	https://pub-a1c2f3559aad439486e2d0fe7766801c.r2.dev/LBY.png',
        visitorTeamId: 'team-8',
        visitorTeamName: 'Equipo H',
        visitorTeamLogoUrl:
          '	https://pub-a1c2f3559aad439486e2d0fe7766801c.r2.dev/BLUE%20LABEL.png',
        homeScore: 1,
        visitorScore: 0,
        isFinished: false,
        winningTeamName: '',
        venue: {
          id: 'venue-8',
          name: 'Estadio Semi 2',
          address: '',
          photoUrl: '',
        },
      },
    ],
  },
  {
    name: 'Semifinal',
    id: 'division-789',
    isFinished: false,
    positions: [],
    tournamentId: 'tournament-456',
    matchesByWeek: [
      {
        id: 'match-9',
        matchDate: '2025-07-09T20:00:00',
        type: TypeMatch.Regular,
        matchWeek: 5,
        homeTeamId: 'team-1',
        homeTeamName: 'Equipo A',
        homeTeamLogoUrl:
          'https://pub-a1c2f3559aad439486e2d0fe7766801c.r2.dev/MAGIOS.png',
        visitorTeamId: 'team-6',
        visitorTeamName: 'Equipo F',
        visitorTeamLogoUrl:
          'https://pub-a1c2f3559aad439486e2d0fe7766801c.r2.dev/TANOS.png',
        homeScore: 2,
        visitorScore: 2,
        isFinished: false,
        winningTeamName: '',
        venue: {
          id: 'venue-9',
          name: 'Estadio Final',
          address: '',
          photoUrl: '',
        },
      },
      {
        id: 'match-10',
        matchDate: '2025-07-10T21:00:00',
        type: TypeMatch.Regular,
        matchWeek: 5,
        homeTeamId: 'team-4',
        homeTeamName: 'Equipo D',
        homeTeamLogoUrl:
          'https://pub-a1c2f3559aad439486e2d0fe7766801c.r2.dev/eep.png',
        visitorTeamId: 'team-8',
        visitorTeamName: 'Equipo H',
        visitorTeamLogoUrl:
          '	https://pub-a1c2f3559aad439486e2d0fe7766801c.r2.dev/BLUE%20LABEL.png',
        homeScore: 1,
        visitorScore: 3,
        isFinished: false,
        winningTeamName: '',
        venue: {
          id: 'venue-10',
          name: 'Estadio Final 2',
          address: '',
          photoUrl: '',
        },
      },
    ],
  },
  {
    name: 'Final',
    id: 'division-234',
    isFinished: false,
    positions: [],
    tournamentId: 'tournament-456',
    matchesByWeek: [
      {
        id: 'match-11',
        matchDate: '2025-07-12T20:30:00',
        type: TypeMatch.Playoff,
        matchWeek: 6,
        homeTeamId: 'team-1',
        homeTeamName: 'Equipo A',
        homeTeamLogoUrl:
          'https://pub-a1c2f3559aad439486e2d0fe7766801c.r2.dev/MAGIOS.png',
        visitorTeamId: 'team-8',
        visitorTeamName: 'Equipo H',
        visitorTeamLogoUrl:
          '	https://pub-a1c2f3559aad439486e2d0fe7766801c.r2.dev/BLUE%20LABEL.png',
        homeScore: 3,
        visitorScore: 2,
        isFinished: true,
        winningTeamName: 'Equipo A',
        venue: {
          id: 'venue-11',
          name: 'Estadio Monumental',
          address: 'Av. Figueroa Alcorta 7597, CABA',
          photoUrl: '',
        },
      },
    ],
  },
];
