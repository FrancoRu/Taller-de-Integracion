import {
  Box,
  Chip,
  Divider,
  List,
  ListItem,
  ListItemText,
  Paper,
  Stack,
  Typography,
} from '@mui/material';
import {
  PlayerHistoryResponse,
  PlayerHistorySeason,
} from '@/modules/playerStatistic/type/playerStatistic';

export interface PlayerHistoryProps {
  history: PlayerHistoryResponse | null;
}

const formatDate = (value?: string | null) => {
  if (!value) {
    return '—';
  }
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime())
    ? '—'
    : parsed.toLocaleDateString('es-AR');
};

const SeasonRow: React.FC<{ season: PlayerHistorySeason }> = ({ season }) => (
  <Paper variant="outlined" sx={{ p: 2 }}>
    <Stack
      direction={{ xs: 'column', sm: 'row' }}
      spacing={1}
      sx={{
        alignItems: { sm: 'center' },
        justifyContent: 'space-between',
        mb: 1,
      }}
    >
      <Box>
        <Typography variant="h6">{season.season}</Typography>
        <Typography variant="subtitle2" sx={{ color: 'text.secondary' }}>
          {season.teamName} · {season.tournamentName}
        </Typography>
      </Box>
      <Stack direction="row" spacing={1}>
        <Chip size="small" label={`${season.totalPoints} pts`} />
        <Chip
          size="small"
          variant="outlined"
          label={`${season.gamesPlayed} PJ`}
        />
      </Stack>
    </Stack>

    <Divider sx={{ my: 1 }} />

    <Typography variant="subtitle2" sx={{ color: 'text.secondary' }}>
      Sanciones
    </Typography>
    {season.sanctions.length > 0 ? (
      <List dense disablePadding>
        {season.sanctions.map(sanction => (
          <ListItem
            key={sanction.sanctionId}
            disableGutters
            secondaryAction={
              <Chip size="small" label={`${sanction.duration} fechas`} />
            }
          >
            <ListItemText
              primary={sanction.description}
              secondary={formatDate(sanction.issuedDate)}
            />
          </ListItem>
        ))}
      </List>
    ) : (
      <Typography variant="body2" sx={{ color: 'text.secondary' }}>
        Sin sanciones esta temporada.
      </Typography>
    )}
  </Paper>
);

/**
 * HU-88: a player's trajectory across seasons. For each season it shows the
 * team, the scoring stats and the sanctions, most recent season first. Purely
 * presentational — the parent fetches the history and passes it down.
 */
const PlayerHistory: React.FC<PlayerHistoryProps> = ({ history }) => {
  if (!history || history.seasons.length === 0) {
    return (
      <Typography variant="body2" sx={{ color: 'text.secondary' }}>
        Este jugador todavía no tiene historial entre temporadas.
      </Typography>
    );
  }

  return (
    <Stack spacing={2}>
      {history.seasons.map(season => (
        <SeasonRow key={`${season.season}-${season.tournamentId}`} season={season} />
      ))}
    </Stack>
  );
};

export default PlayerHistory;
