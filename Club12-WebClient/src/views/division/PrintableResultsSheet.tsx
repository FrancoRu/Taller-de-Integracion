import { useEffect, useState } from 'react';
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
import { Position } from '@/modules/division/type/division.d';
import { sortPositions } from '@/modules/division/utils/sortPositions';
import { downloadCsv } from '@/modules/core/utils/csv';
import ExportCsvButton from '@/views/core/components/ExportCsvButton';

interface PrintableResultsSheetProps {
  divisionName?: string;
  positions: Position[];
}

const STANDINGS_CSV_HEADERS = [
  '#',
  'Equipo',
  'PJ',
  'PG',
  'PP',
  'GF',
  'GC',
  'DIF',
  'Pts',
];

const standingsCsvRows = (rows: Position[]) =>
  rows.map((row, index) => [
    index + 1,
    row.teamName,
    row.matchesPlayed,
    row.wins,
    row.losses,
    row.pointsFor,
    row.pointsAgainst,
    row.pointsDifference,
    row.points,
  ]);

/** Slugifies a division name into a safe CSV filename fragment. */
const csvFilenamePart = (divisionName?: string) =>
  (divisionName ?? 'division')
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .replace(/[^a-zA-Z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .toLowerCase() || 'division';

/**
 * `@media print` isolation: hide every element on the page except this
 * component's `[data-print="sheet"]` subtree, and force that subtree
 * visible even though it is `display:none` on screen. This hides all app
 * chrome (nav/tabs/buttons) without needing to tag them individually.
 */
const printMediaStyles = {
  '@media print': {
    'body *': { visibility: 'hidden' },
    '[data-print="sheet"], [data-print="sheet"] *': { visibility: 'visible' },
    '[data-print="sheet"]': {
      display: 'block !important',
      position: 'absolute',
      top: 0,
      left: 0,
      width: '100%',
    },
    '[data-print="hide"]': { display: 'none !important' },
    thead: { display: 'table-header-group' },
    tr: { breakInside: 'avoid' },
    '*': {
      printColorAdjust: 'exact',
      WebkitPrintColorAdjust: 'exact',
    },
  },
};

/**
 * Native-print-only sheet for a division's standings (posiciones). Renders an
 * on-screen control bar ("Imprimir" + "Exportar CSV") plus a hidden sheet
 * that only becomes visible via `window.print()` / `@media print`. Zero
 * PDF/print dependencies. Goleadores (top scorers) has its own dedicated
 * tab/sheet — this component is standings-only.
 */
export default function PrintableResultsSheet({
  divisionName,
  positions,
}: PrintableResultsSheetProps) {
  const [isPrintTarget, setIsPrintTarget] = useState(false);

  /**
   * A tournament page renders one PrintableResultsSheet per division, each
   * with its own `[data-print="sheet"]` node. If every instance carried that
   * attribute at once, the print stylesheet would make ALL of them visible
   * and absolutely-positioned at the same top-left corner simultaneously,
   * producing overlapping/garbled output. Only the instance whose "Imprimir"
   * button was clicked gets the attribute, so it's the only one printed.
   */
  useEffect(() => {
    if (!isPrintTarget) return;
    window.print();
    setIsPrintTarget(false);
  }, [isPrintTarget]);

  const standingsRows = sortPositions(positions);

  const handleExportCsv = () =>
    downloadCsv(
      `posiciones-${csvFilenamePart(divisionName)}`,
      STANDINGS_CSV_HEADERS,
      standingsCsvRows(standingsRows)
    );

  return (
    <Box>
      <GlobalStyles styles={printMediaStyles} />

      <Box
        data-print="hide"
        sx={{
          display: "flex",
          alignItems: "center",
          gap: 1.5,
          flexWrap: "wrap",
          mb: 2
        }}>
        <Button
          variant="contained"
          size="small"
          startIcon={<PrintIcon />}
          onClick={() => setIsPrintTarget(true)}
          sx={{ height: 32, minHeight: 32 }}
        >
          Imprimir
        </Button>
        <ExportCsvButton onExport={handleExportCsv} />
      </Box>

      <Box data-print={isPrintTarget ? 'sheet' : undefined} sx={{ display: 'none' }}>
        {divisionName && (
          <Typography variant="h6" component="h1" gutterBottom>
            {divisionName}
          </Typography>
        )}

        <Box>
          <Typography variant="subtitle1" component="h2" gutterBottom>
            Posiciones
          </Typography>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>#</TableCell>
                  <TableCell>Equipo</TableCell>
                  <TableCell align="center">PJ</TableCell>
                  <TableCell align="center">PG</TableCell>
                  <TableCell align="center">PP</TableCell>
                  <TableCell align="center">Pts</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {standingsRows.map((row, index) => (
                  <TableRow key={row.teamId}>
                    <TableCell>{index + 1}</TableCell>
                    <TableCell>{row.teamName}</TableCell>
                    <TableCell align="center">{row.matchesPlayed}</TableCell>
                    <TableCell align="center">{row.wins}</TableCell>
                    <TableCell align="center">{row.losses}</TableCell>
                    <TableCell align="center">{row.points}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Box>
      </Box>
    </Box>
  );
}
