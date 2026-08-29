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
import { alpha } from '@mui/material/styles';
import { GUID } from '@/modules/core/types/types';
import { Position, QualificationRange } from '@/modules/division/type/division.d';
import { sortPositions } from '@/modules/division/utils/sortPositions';
import {
  cupTierColor,
  cupTierMarker,
  findQualificationRange,
} from '@/modules/division/utils/qualificationRange';
import PrintableResultsSheet from '@/views/division/PrintableResultsSheet';
import TeamLogo from '@/views/core/components/TeamLogo';

interface DivisionStandingsProps {
  positions?: Position[];
  divisionId?: GUID;
  divisionName?: string;
  /**
   * The position ranges that qualify to a playoff cup (HU-45). When present,
   * the qualifying rows are tinted by cup tier and a legend is shown below the
   * table. May be empty (e.g. the multi-group cup path has no per-group ranges).
   */
  qualificationRanges?: QualificationRange[];
}

const DivisionStandings: React.FC<DivisionStandingsProps> = ({
  positions,
  divisionId,
  divisionName,
  qualificationRanges,
}) => {
  const rows = useMemo(() => sortPositions(positions ?? []), [positions]);
  // The legend lists only the ranges actually present, in top-down cup order.
  const legendRanges = useMemo(
    () => [...(qualificationRanges ?? [])].sort((a, b) => a.order - b.order),
    [qualificationRanges]
  );

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
              <TableCell align="center">#</TableCell>
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
            {rows.map((row, index) => {
              const range = findQualificationRange(qualificationRanges, index + 1);
              const tierColor = range ? cupTierColor(range.order) : undefined;
              return (
              <TableRow
                key={row.teamId}
                hover
                // Subtle qualification highlight (HU-45): a colored left border
                // plus a faint tint of the same cup-tier color, readable on the
                // dark theme. The row's title names the cup so the meaning does
                // not rely on color alone.
                title={range ? `Clasifica a ${range.cupName}` : undefined}
                sx={
                  tierColor
                    ? {
                        backgroundColor: alpha(tierColor, 0.12),
                        '& > td:first-of-type': {
                          borderLeft: `4px solid ${tierColor}`,
                        },
                      }
                    : undefined
                }
              >
                <TableCell align="center">{index + 1}</TableCell>
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
                  {row.pointDeduction && (
                    <Tooltip
                      title={`Deducción de ${row.pointDeduction.points} ${
                        row.pointDeduction.points === 1 ? 'punto' : 'puntos'
                      }: ${row.pointDeduction.reason}`}
                    >
                      <Typography
                        component="span"
                        variant="caption"
                        sx={{
                          display: 'block',
                          color: 'error.main',
                          fontWeight: 600,
                          lineHeight: 1,
                        }}
                        aria-label={`Deducción de ${row.pointDeduction.points} puntos: ${row.pointDeduction.reason}`}
                      >
                        -{row.pointDeduction.points}
                      </Typography>
                    </Tooltip>
                  )}
                </TableCell>
              </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </TableContainer>
      {legendRanges.length > 0 && (
        <Stack
          direction="row"
          spacing={2}
          useFlexGap
          sx={{ flexWrap: 'wrap', mt: 1.5, px: 1 }}
          aria-label="Referencias de clasificación"
        >
          {legendRanges.map(range => (
            <Stack
              key={`${range.cupName}-${range.order}`}
              direction="row"
              spacing={0.75}
              sx={{ alignItems: 'center' }}
            >
              <Box
                component="span"
                aria-hidden
                sx={{
                  width: 12,
                  height: 12,
                  borderRadius: 0.5,
                  bgcolor: cupTierColor(range.order),
                  flexShrink: 0,
                }}
              />
              <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                {cupTierMarker(range.order)} {range.cupName} ({range.fromPosition}-{range.toPosition})
              </Typography>
            </Stack>
          ))}
        </Stack>
      )}
    </Box>
  );
};

export default DivisionStandings;
