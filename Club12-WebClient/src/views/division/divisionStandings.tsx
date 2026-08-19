import { useMemo } from 'react';
import {
  Box,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tooltip,
  Typography,
} from '@mui/material';
import { GUID } from '@/modules/core/types/types';
import { Position } from '@/modules/division/type/division.d';
import { sortPositions } from '@/modules/division/utils/sortPositions';
import PrintableResultsSheet from '@/views/division/PrintableResultsSheet';
import TeamLogo from '@/views/core/components/TeamLogo';

interface DivisionStandingsProps {
  positions?: Position[];
  divisionId?: GUID;
  divisionName?: string;
}

const DivisionStandings: React.FC<DivisionStandingsProps> = ({
  positions,
  divisionId,
  divisionName,
}) => {
  const rows = useMemo(() => sortPositions(positions ?? []), [positions]);

  if (rows.length === 0) {
    return (
      <Typography variant="body2" sx={{
        color: "text.secondary"
      }}>Todavía no hay posiciones para esta división.
              </Typography>
    );
  }

  return (
    <Box>
      {divisionId && (
        <PrintableResultsSheet
          divisionId={divisionId}
          divisionName={divisionName}
          positions={rows}
        />
      )}
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
                <Box component="span" sx={{
                  fontWeight: 700
                }}>
                  Pts
                </Box>
              </TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {rows.map((row, index) => (
              <TableRow key={row.teamId} hover>
                <TableCell>{index + 1}</TableCell>
                <TableCell>
                  <Stack direction="row" spacing={1.5} sx={{
                    alignItems: "center"
                  }}>
                    <TeamLogo teamName={row.teamName} logoUrl={row.logoUrl} size={24} />
                    <Box component="span">{row.teamName}</Box>
                  </Stack>
                </TableCell>
                <TableCell align="center">{row.matchesPlayed}</TableCell>
                <TableCell align="center">{row.wins}</TableCell>
                <TableCell align="center">{row.losses}</TableCell>
                <TableCell align="center">{row.pointsFor}</TableCell>
                <TableCell align="center">{row.pointsAgainst}</TableCell>
                <TableCell align="center">{row.pointsDifference}</TableCell>
                <TableCell align="center">
                  <Box component="span" sx={{
                    fontWeight: 700
                  }}>
                    {row.points}
                  </Box>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Box>
  );
};

export default DivisionStandings;
