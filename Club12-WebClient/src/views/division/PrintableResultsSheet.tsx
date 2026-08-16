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
import { GUID } from '@/modules/core/types/types';
import {
  DivisionTopScoreResponse,
  Position,
} from '@/modules/division/type/division.d';
import { useDivision } from '@/modules/division/hook/division.hook';
import { sortPositions } from '@/modules/division/utils/sortPositions';

type PrintTarget = 'standings' | 'goleadores' | 'both';

interface PrintableResultsSheetProps {
  divisionId?: GUID;
  divisionName?: string;
  positions: Position[];
}

const sortTopScores = (scores: DivisionTopScoreResponse[]) =>
  [...scores].sort((a, b) => b.totalPoints - a.totalPoints);

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
  const { getTopScoresByDivisionId } = useDivision();
  const [target, setTarget] = useState<PrintTarget>('standings');
  const [topScores, setTopScores] = useState<DivisionTopScoreResponse[]>([]);

  useEffect(() => {
    if (!divisionId) return;
    const fetchTopScores = async () => {
      const response = await getTopScoresByDivisionId(divisionId);
      setTopScores(response ?? []);
    };
    void fetchTopScores();
  }, [divisionId, getTopScoresByDivisionId]);

  const standingsRows = useMemo(() => sortPositions(positions), [positions]);
  const topScoreRows = useMemo(() => sortTopScores(topScores), [topScores]);

  const showStandings = target === 'standings' || target === 'both';
  const showGoleadores = target === 'goleadores' || target === 'both';

  return (
    <Box>
      <GlobalStyles styles={printMediaStyles} />

      <Box
        data-print="hide"
        display="flex"
        alignItems="center"
        gap={2}
        flexWrap="wrap"
        sx={{ mb: 2 }}
      >
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
        <Button variant="contained" onClick={() => window.print()}>
          Imprimir
        </Button>
      </Box>

      <Box data-print="sheet" sx={{ display: 'none' }}>
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
                  {topScoreRows.map((row, index) => (
                    <TableRow key={row.playerId}>
                      <TableCell>{index + 1}</TableCell>
                      <TableCell>{`${row.firstName} ${row.lastName}`}</TableCell>
                      <TableCell align="center">{row.totalPoints}</TableCell>
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
