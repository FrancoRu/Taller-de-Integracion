import {
  Grid,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import { PlayerStatisticCardResponse } from '@/modules/playerStatistic/type/playerStatistic';

export interface PlayerStatisticCardProps {
  card: PlayerStatisticCardResponse | null;
}

const StatTile: React.FC<{ label: string; value: number | string }> = ({
  label,
  value,
}) => (
  <Paper variant="outlined" sx={{ p: 2, textAlign: 'center' }}>
    <Typography variant="h5">{value}</Typography>
    <Typography variant="subtitle2" sx={{ color: 'text.secondary' }}>
      {label}
    </Typography>
  </Paper>
);

/**
 * HU-87: a player's statistic card. Shows the overall totals up top and a
 * per-season breakdown (season, games, total, average) below. Purely
 * presentational — the parent fetches the card and passes it down.
 */
const PlayerStatisticCard: React.FC<PlayerStatisticCardProps> = ({ card }) => {
  if (!card) {
    return (
      <Typography variant="body2" sx={{ color: 'text.secondary' }}>
        Este jugador todavía no tiene estadísticas registradas.
      </Typography>
    );
  }

  return (
    <>
      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid size={{ xs: 12, sm: 4 }}>
          <StatTile label="Puntos totales" value={card.totalPoints} />
        </Grid>
        <Grid size={{ xs: 12, sm: 4 }}>
          <StatTile label="Partidos jugados" value={card.gamesPlayed} />
        </Grid>
        <Grid size={{ xs: 12, sm: 4 }}>
          <StatTile label="Promedio" value={card.averagePoints} />
        </Grid>
      </Grid>

      <Typography variant="subtitle1" sx={{ mb: 1 }}>
        Por temporada
      </Typography>

      {card.seasons.length > 0 ? (
        <TableContainer component={Paper} variant="outlined">
          <Table size="small" aria-label="Estadísticas por temporada">
            <TableHead>
              <TableRow>
                <TableCell>Temporada</TableCell>
                <TableCell align="right">Partidos</TableCell>
                <TableCell align="right">Total</TableCell>
                <TableCell align="right">Promedio</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {card.seasons.map(season => (
                <TableRow key={season.season}>
                  <TableCell>{season.season}</TableCell>
                  <TableCell align="right">{season.gamesPlayed}</TableCell>
                  <TableCell align="right">{season.totalPoints}</TableCell>
                  <TableCell align="right">{season.averagePoints}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      ) : (
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          Este jugador todavía no tiene estadísticas por temporada.
        </Typography>
      )}
    </>
  );
};

export default PlayerStatisticCard;
