import { useEffect, useMemo, useState } from 'react';
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
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from '@mui/material';
import PrintIcon from '@mui/icons-material/Print';
import { GUID } from '@/modules/core/types/types';
import { Position } from '@/modules/division/type/division.d';
import { sortPositions } from '@/modules/division/utils/sortPositions';
import { scorerService } from '@/modules/scorer/service/scorer.service';
import { IScorerByPlayerResponse } from '@/modules/scorer/type/scorer.d';

const TOP_SCORES_PAGE_SIZE = 100;

type PrintTarget = 'standings' | 'goleadores' | 'both';

interface PrintableResultsSheetProps {
  divisionId?: GUID;
  divisionName?: string;
  positions: Position[];
}

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
 * Native-print-only sheet for a division's standings and/or goleadores
 * (top scorers). Renders an on-screen control bar (target toggle +
 * "Imprimir" button) plus a hidden sheet that only becomes visible via
 * `window.print()` / `@media print`. Zero PDF/print dependencies.
 */
export default function PrintableResultsSheet({
  divisionId,
  divisionName,
  positions,
}: PrintableResultsSheetProps) {
  const [target, setTarget] = useState<PrintTarget>('standings');
  const [topScores, setTopScores] = useState<IScorerByPlayerResponse[]>([]);
  const [isPrintTarget, setIsPrintTarget] = useState(false);

  useEffect(() => {
    if (!divisionId) return;
    const fetchTopScores = async () => {
      const response = await scorerService.getScorersByPlayerFiltered({
        divisionId,
        pageSize: TOP_SCORES_PAGE_SIZE,
        pageNumber: 1,
      });
      setTopScores(response.data?.items ?? []);
    };
    void fetchTopScores();
  }, [divisionId]);

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

  const standingsRows = useMemo(() => sortPositions(positions), [positions]);

  const showStandings = target === 'standings' || target === 'both';
  const showGoleadores = target === 'goleadores' || target === 'both';

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
        <PrintIcon fontSize="small" sx={{ color: 'text.secondary' }} />
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          Imprimir:
        </Typography>
        <ToggleButtonGroup
          exclusive
          size="small"
          value={target}
          onChange={(_, value: PrintTarget | null) => {
            if (value) setTarget(value);
          }}
        >
          <ToggleButton value="standings">Posiciones</ToggleButton>
          <ToggleButton value="goleadores">Goleadores</ToggleButton>
          <ToggleButton value="both">Ambos</ToggleButton>
        </ToggleButtonGroup>
        <Button
          variant="contained"
          size="small"
          startIcon={<PrintIcon />}
          onClick={() => setIsPrintTarget(true)}
        >
          Imprimir
        </Button>
      </Box>

      <Box data-print={isPrintTarget ? 'sheet' : undefined} sx={{ display: 'none' }}>
        {divisionName && (
          <Typography variant="h6" component="h1" gutterBottom>
            {divisionName}
          </Typography>
        )}

        {showStandings && (
          <Box sx={{ mb: showGoleadores ? 4 : 0 }}>
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
        )}

        {showGoleadores && (
          <Box>
            <Typography variant="subtitle1" component="h2" gutterBottom>
              Goleadores
            </Typography>
            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>#</TableCell>
                    <TableCell>Jugador</TableCell>
                    <TableCell align="center">Puntos</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {topScores.map((row, index) => (
                    <TableRow key={row.playerId}>
                      <TableCell>{index + 1}</TableCell>
                      <TableCell>{row.fullName}</TableCell>
                      <TableCell align="center">{row.points}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          </Box>
        )}
      </Box>
    </Box>
  );
}
