import { useMemo } from 'react';
import {
  Box,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tooltip,
  Typography,
} from '@mui/material';
import { Position } from '@/modules/division/type/division.d';

interface DivisionStandingsProps {
  positions?: Position[];
}

const sortPositions = (positions: Position[]) =>
  [...positions].sort((a, b) => {
    if (b.points !== a.points) {
      return b.points - a.points;
    }
    if (b.pointsDifference !== a.pointsDifference) {
      return b.pointsDifference - a.pointsDifference;
    }
    return b.pointsFor - a.pointsFor;
  });

const DivisionStandings: React.FC<DivisionStandingsProps> = ({
  positions,
}) => {
  const rows = useMemo(() => sortPositions(positions ?? []), [positions]);

  if (rows.length === 0) {
    return (
      <Typography variant="body2" color="text.secondary">
        Todavía no hay posiciones para esta división.
      </Typography>
    );
  }

  return (
    <TableContainer>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>#</TableCell>
            <TableCell>Equipo</TableCell>
            <Tooltip title="Partidos jugados">
              <TableCell align="center">PJ</TableCell>
            </Tooltip>
            <Tooltip title="Partidos ganados">
              <TableCell align="center">PG</TableCell>
            </Tooltip>
            <Tooltip title="Partidos perdidos">
              <TableCell align="center">PP</TableCell>
            </Tooltip>
            <Tooltip title="Puntos a favor">
              <TableCell align="center">GF</TableCell>
            </Tooltip>
            <Tooltip title="Puntos en contra">
              <TableCell align="center">GC</TableCell>
            </Tooltip>
            <Tooltip title="Diferencia">
              <TableCell align="center">DIF</TableCell>
            </Tooltip>
            <TableCell align="center">
              <Box component="span" fontWeight={700}>
                Pts
              </Box>
            </TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {rows.map((row, index) => (
            <TableRow key={row.teamid} hover>
              <TableCell>{index + 1}</TableCell>
              <TableCell>{row.teamName}</TableCell>
              <TableCell align="center">{row.matchesPlayed}</TableCell>
              <TableCell align="center">{row.wins}</TableCell>
              <TableCell align="center">{row.losses}</TableCell>
              <TableCell align="center">{row.pointsFor}</TableCell>
              <TableCell align="center">{row.pointsAgainst}</TableCell>
              <TableCell align="center">{row.pointsDifference}</TableCell>
              <TableCell align="center">
                <Box component="span" fontWeight={700}>
                  {row.points}
                </Box>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
};

export default DivisionStandings;
