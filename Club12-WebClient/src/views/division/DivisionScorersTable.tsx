import { useEffect, useRef, useState } from 'react';
import {
  Box,
  Button,
  GlobalStyles,
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

const CSV_HEADERS = ['#', 'Jugador', 'Dorsal', 'Puntos', 'Equipo'];

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
        pageSize: FILTER_OPTIONS_PAGE_SIZE,
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
  }, [divisionId]);

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
                  <TableCell align="center">Dorsal</TableCell>
                  <TableCell align="center">Puntos</TableCell>
                  <TableCell>Equipo</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {scorers.map((row, index) => (
                  <TableRow key={row.playerId} hover>
                    <TableCell>{index + 1}</TableCell>
                    <TableCell>{row.fullName}</TableCell>
                    <TableCell align="center">{row.jerseyNumber ?? '—'}</TableCell>
                    <TableCell align="center">
                      <Box component="span" sx={{ fontWeight: 700 }}>
                        {row.points}
                      </Box>
                    </TableCell>
                    <TableCell>{row.teamName ?? '—'}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Box>
      )}
    </Box>
  );
}
