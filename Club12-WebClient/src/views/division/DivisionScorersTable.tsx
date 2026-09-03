import { useEffect, useRef, useState } from 'react';
import {
  Box,
  Button,
  GlobalStyles,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import PrintIcon from '@mui/icons-material/Print';
import { GUID } from '@/modules/core/types/types';
import { scorerService } from '@/modules/scorer/service/scorer.service';
import { IScorerByPlayerResponse } from '@/modules/scorer/type/scorer.d';
import { downloadCsv } from '@/modules/core/utils/csv';
import { printMediaStyles } from '@/modules/core/utils/printStyles';
import ExportCsvButton from '@/views/core/components/ExportCsvButton';
import { TableSkeleton } from '@/views/core/components/skeletons';
import { FILTER_OPTIONS_PAGE_SIZE } from '@/modules/core/constants/pagination';
import TeamLogo from '@/views/core/components/TeamLogo';
import JerseySvg from '@/views/core/components/JerseySvg';
import { brand } from '@/design/tokens';

const CSV_HEADERS = ['#', 'Jugador', 'Dorsal', 'Puntos', 'Equipo'];

/** Subtle medal accents for the top three ranks — the same as the podium's. */
const RANK_ACCENT: Record<number, string> = {
  1: brand.gold,
  2: '#C7CDD6',
  3: '#CD8E5A',
};

const csvRows = (rows: IScorerByPlayerResponse[]) =>
  rows.map((row, index) => [
    index + 1,
    row.fullName,
    row.jerseyNumber ?? '—',
    row.points,
    row.teamName ?? '—',
  ]);

/** Slugifies a division name into a safe CSV/print filename fragment. */
const filenamePart = (divisionName?: string) =>
  (divisionName ?? 'division')
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .replace(/[^a-zA-Z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .toLowerCase() || 'division';

interface DivisionScorersTableProps {
  divisionId: GUID;
  divisionName?: string;
  /**
   * Caps how many ranked scorers are fetched/shown (e.g. the public
   * tournament page's top 10) instead of the full division. Omit for the
   * complete ranking — the admin panel's own use of this table.
   */
  limit?: number;
}

/**
 * A division's top-scorer ranking (goleadores): jugador, dorsal, puntos,
 * equipo, sorted by puntos (the backend ranking is already ordered
 * descending). Mirrors PrintableResultsSheet's Imprimir/Exportar CSV
 * affordances, kept as its own sheet since goleadores and posiciones are
 * printed/exported independently now (HU-89).
 */
export default function DivisionScorersTable({
  divisionId,
  divisionName,
  limit,
}: DivisionScorersTableProps) {
  const [scorers, setScorers] = useState<IScorerByPlayerResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [isPrintTarget, setIsPrintTarget] = useState(false);

  const divisionIdRef = useRef(divisionId);
  divisionIdRef.current = divisionId;

  useEffect(() => {
    let cancelled = false;

    const fetchScorers = async () => {
      setLoading(true);
      const response = await scorerService.getScorersByPlayerFiltered({
        divisionId: divisionIdRef.current,
        pageSize: limit ?? FILTER_OPTIONS_PAGE_SIZE,
        pageNumber: 1,
      });
      if (!cancelled) {
        setScorers(response.data?.items ?? []);
        setLoading(false);
      }
    };

    void fetchScorers();
    return () => {
      cancelled = true;
    };
  }, [divisionId, limit]);

  // Same single-instance-at-a-time print guard PrintableResultsSheet uses:
  // the sheet only becomes visible to `window.print()` once "Imprimir" is
  // clicked, via the shared [data-print] convention (printMediaStyles).
  useEffect(() => {
    if (!isPrintTarget) return;
    window.print();
    setIsPrintTarget(false);
  }, [isPrintTarget]);

  const handleExportCsv = () =>
    downloadCsv(
      `goleadores-${filenamePart(divisionName)}`,
      CSV_HEADERS,
      csvRows(scorers)
    );

  if (loading) {
    return <TableSkeleton rows={6} columns={5} />;
  }

  return (
    <Box>
      <GlobalStyles styles={printMediaStyles} />

      <Box
        data-print="hide"
        sx={{ display: 'flex', alignItems: 'center', gap: 1.5, flexWrap: 'wrap', mb: 2 }}
      >
        <Button
          variant="contained"
          size="small"
          startIcon={<PrintIcon />}
          onClick={() => setIsPrintTarget(true)}
          disabled={scorers.length === 0}
          sx={{ height: 32, minHeight: 32 }}
        >
          Imprimir
        </Button>
        <ExportCsvButton onExport={handleExportCsv} disabled={scorers.length === 0} />
      </Box>

      {scorers.length === 0 ? (
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          Todavía no hay goleadores registrados en esta división.
        </Typography>
      ) : (
        <Box data-print={isPrintTarget ? 'sheet' : undefined}>
          {divisionName && (
            // Only meaningful on the printed sheet, which has no PageShell
            // title to identify the division — hidden on screen, where the
            // page's own title already does that job.
            <Typography
              variant="h6"
              component="h1"
              gutterBottom
              sx={{ display: 'none', '@media print': { display: 'block' } }}
            >
              {divisionName} — Goleadores
            </Typography>
          )}
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>#</TableCell>
                  <TableCell>Jugador</TableCell>
                  <TableCell align="center">Camiseta</TableCell>
                  <TableCell align="center">Puntos</TableCell>
                  <TableCell>Equipo</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {scorers.map((row, index) => {
                  const rank = index + 1;
                  const accent = RANK_ACCENT[rank];

                  return (
                    <TableRow key={row.playerId} hover>
                      <TableCell>
                        <Box
                          component="span"
                          sx={{
                            display: 'inline-flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            width: 24,
                            height: 24,
                            borderRadius: '50%',
                            fontWeight: accent ? 700 : 500,
                            color: accent ?? 'text.secondary',
                            border: accent ? `1.5px solid ${accent}` : 'none',
                          }}
                        >
                          {rank}
                        </Box>
                      </TableCell>
                      <TableCell sx={{ fontWeight: 500 }}>{row.fullName}</TableCell>
                      <TableCell align="center">
                        {/* The jersey already prints the dorsal on the chest —
                            no separate number column needed alongside it. */}
                        <Box sx={{ display: 'inline-flex' }}>
                          <JerseySvg
                            color={row.teamShirtColor}
                            secondaryColor={row.teamShirtSecondaryColor}
                            tertiaryColor={row.teamShirtTertiaryColor}
                            style={row.teamJerseyStyle}
                            number={row.jerseyNumber}
                            size={30}
                            title={`Camiseta de ${row.teamName ?? 'el equipo'}${row.jerseyNumber != null ? `, dorsal ${row.jerseyNumber}` : ''}`}
                          />
                        </Box>
                      </TableCell>
                      <TableCell align="center">
                        <Box component="span" sx={{ fontWeight: 700 }}>
                          {row.points}
                        </Box>
                      </TableCell>
                      <TableCell>
                        <Stack direction="row" spacing={1} sx={{ alignItems: 'center', minWidth: 0 }}>
                          <TeamLogo teamName={row.teamName ?? '?'} logoUrl={row.teamLogoUrl} size={24} />
                          <Typography variant="body2" noWrap sx={{ minWidth: 0 }}>
                            {row.teamName ?? '—'}
                          </Typography>
                        </Stack>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </TableContainer>
        </Box>
      )}
    </Box>
  );
}
