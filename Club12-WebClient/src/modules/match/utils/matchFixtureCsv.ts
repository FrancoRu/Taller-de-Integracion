import { IMatchResponse } from '@/modules/match/type/match.d';
import {
  formatRoundLabel,
  groupMatchesByRound,
} from '@/modules/match/utils/matchGrouping';
import { formatDateTimeAr } from '@/modules/core/utils/formatDate';
import { CsvRow } from '@/modules/core/utils/csv';

/** Column headers (HU-89) for the fixture CSV export. */
export const FIXTURE_CSV_HEADERS = [
  'Fecha',
  'Fecha y hora',
  'Local',
  'Visitante',
  'Resultado',
  'Estado',
];

const EMPTY_TEAM = '—';

/** Builds the CSV rows (HU-89) for a fixture, grouped and ordered by round. */
export const buildFixtureCsvRows = (matches: IMatchResponse[]): CsvRow[] =>
  groupMatchesByRound(matches).flatMap(round =>
    round.matches.map(match => [
      formatRoundLabel(round.round),
      formatDateTimeAr(match.matchDate),
      match.homeTeam?.name ?? EMPTY_TEAM,
      match.visitorTeam?.name ?? EMPTY_TEAM,
      match.isFinished
        ? `${match.homeTeam?.score ?? 0}-${match.visitorTeam?.score ?? 0}`
        : EMPTY_TEAM,
      match.isFinished ? 'Finalizado' : 'Programado',
    ])
  );
