import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Box,
  Button,
  Card,
  CardContent,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { Link } from 'react-router-dom';
import { notifySuccess } from '@/modules/core/utils/confirmDialog';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { IMatchResponse } from '@/modules/match/type/match';
import { useMatch } from '@/modules/match/hook/match.hook';
import { ITeamMatchResponse } from '@/modules/team/type/team';
import { IPublicPlayerResponse } from '@/modules/player/type/player.d';
import { usePlayerStatistic } from '@/modules/playerStatistic/hook/playerStatistic.hook';
import { PlayerStatisticResponse } from '@/modules/playerStatistic/type/playerStatistic';
import { TableSkeleton } from '@/views/core/components/skeletons';
import { FILTER_OPTIONS_PAGE_SIZE } from '@/modules/core/constants/pagination';
import HabilitacionBadge from '@/views/medicalRecord/HabilitacionBadge';
import { resolveIsHabilitado } from '@/modules/medicalRecord/utils/medicalRecordDisplay';
import TeamLogo from '@/views/core/components/TeamLogo';
import JerseySvg from '@/views/core/components/JerseySvg';
import { font } from '@/design/tokens';
import { ScoreEmphasis } from '@/modules/match/utils/matchDisplay';

interface MatchStatisticsTabProps {
  match: IMatchResponse;
}

/** Same reading as the public match page's finished scoreboard, but derived
 *  live from what's currently typed rather than from a recorded winner —
 *  the leading side reads ahead while the sheets are still being filled in. */
const scoreColor: Record<ScoreEmphasis, string> = {
  winner: 'primary.main',
  loser: 'text.disabled',
  neutral: 'text.primary',
};

const CREST_SIZE = 56;

/**
 * Sums the Points statistics for a match, keyed by player id, so each team's
 * currently-loaded planilla can be shown without a second request.
 */
const buildPointsMap = (
  statistics: PlayerStatisticResponse[]
): Record<string, number> =>
  statistics
    .filter(stat => stat.type === 'Points')
    .reduce<Record<string, number>>((acc, stat) => {
      acc[stat.playerId] = (acc[stat.playerId] ?? 0) + stat.value;
      return acc;
    }, {});

const buildFormFromPoints = (
  players: IPublicPlayerResponse[],
  pointsByPlayer: Record<string, number>
): Record<string, string> =>
  players.reduce<Record<string, string>>((acc, player) => {
    acc[player.id] = String(pointsByPlayer[player.id] ?? 0);
    return acc;
  }, {});

const sumForm = (form: Record<string, string>): number =>
  Object.values(form).reduce((total, value) => total + (Number(value) || 0), 0);

/**
 * Loads a match's result by entering each player's points for BOTH teams in
 * one place (HU-72): the final score is derived as the sum of what each
 * team's players scored, instead of being typed in separately and then
 * checked against a sheet loaded here afterward. This is the only place a
 * match's result is loaded — the score is always what the players add up to.
 *
 * Styled as a live scoreboard (crest, big Oswald score, box score below) —
 * the same broadcast-scoreboard language the public match page already uses
 * once a game is finished, applied here while it's still being entered.
 */
export default function MatchStatisticsTab({ match }: MatchStatisticsTabProps) {
  const { loadMatchResultFromSheets } = useMatch();
  const { getPlayerStatisticsByFilter } = usePlayerStatistic();

  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [homeForm, setHomeForm] = useState<Record<string, string>>({});
  const [visitorForm, setVisitorForm] = useState<Record<string, string>>({});

  const getStatisticsRef = useRef(getPlayerStatisticsByFilter);
  useEffect(() => {
    getStatisticsRef.current = getPlayerStatisticsByFilter;
  }, [getPlayerStatisticsByFilter]);

  const homePlayers = useMemo(() => match.homeTeam?.players ?? [], [match.homeTeam]);
  const visitorPlayers = useMemo(
    () => match.visitorTeam?.players ?? [],
    [match.visitorTeam]
  );

  const loadStatistics = useCallback(async () => {
    setLoading(true);
    const response = await getStatisticsRef.current({
      matchId: match.id,
      pageSize: FILTER_OPTIONS_PAGE_SIZE,
      pageNumber: 1,
    });
    const items = response?.items ?? [];

    const pointsByPlayer = buildPointsMap(items);
    setHomeForm(buildFormFromPoints(homePlayers, pointsByPlayer));
    setVisitorForm(buildFormFromPoints(visitorPlayers, pointsByPlayer));
    setLoading(false);
    // homePlayers/visitorPlayers are derived from `match` on every render;
    // re-running this whenever the match's own id changes is enough and
    // avoids re-fetching on every unrelated match update.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [match.id]);

  useEffect(() => {
    void loadStatistics();
  }, [loadStatistics]);

  const homeSum = useMemo(() => sumForm(homeForm), [homeForm]);
  const visitorSum = useMemo(() => sumForm(visitorForm), [visitorForm]);
  const isTie = homeSum === visitorSum;
  const rostersReady = homePlayers.length > 0 && visitorPlayers.length > 0;

  const emphasis: { home: ScoreEmphasis; visitor: ScoreEmphasis } = isTie
    ? { home: 'neutral', visitor: 'neutral' }
    : homeSum > visitorSum
      ? { home: 'winner', visitor: 'loser' }
      : { home: 'loser', visitor: 'winner' };

  const handleSaveResult = useCallback(async () => {
    if (!rostersReady || isTie) {
      return;
    }

    setSubmitting(true);
    const result = await loadMatchResultFromSheets(match.id, {
      homeScores: homePlayers.map(player => ({
        playerId: player.id,
        points: Number(homeForm[player.id]) || 0,
      })),
      visitorScores: visitorPlayers.map(player => ({
        playerId: player.id,
        points: Number(visitorForm[player.id]) || 0,
      })),
    });
    setSubmitting(false);

    // A falsy result means the backend rejected the sheets (e.g. a tied sum,
    // an ineligible player, or a 409); the message is surfaced globally and
    // the form stays as-is so the operator can correct it.
    if (!result) {
      return;
    }

    await loadStatistics();
    await notifySuccess({ title: 'Resultado cargado' });
  }, [
    rostersReady,
    isTie,
    loadMatchResultFromSheets,
    match.id,
    homePlayers,
    visitorPlayers,
    homeForm,
    visitorForm,
    loadStatistics,
  ]);

  const renderScoreNumber = (value: number, side: ScoreEmphasis) => (
    <Typography
      component="span"
      sx={{
        fontFamily: font.display,
        fontWeight: 700,
        fontSize: { xs: '2.5rem', md: '3.25rem' },
        lineHeight: 1,
        color: scoreColor[side],
        transition: 'color 0.15s ease',
      }}
    >
      {value}
    </Typography>
  );

  const renderTeamHeader = (
    team: ITeamMatchResponse | null,
    fallbackLabel: string,
    side: ScoreEmphasis
  ) => (
    <Stack spacing={1} sx={{ alignItems: 'center', flex: 1, minWidth: 0, textAlign: 'center' }}>
      <TeamLogo teamName={team?.name ?? fallbackLabel} logoUrl={team?.logoUrl} size={CREST_SIZE} />
      <Typography
        variant="subtitle1"
        noWrap
        sx={{
          maxWidth: '100%',
          fontWeight: side === 'winner' ? 700 : 600,
          color: side === 'loser' ? 'text.secondary' : 'text.primary',
        }}
      >
        {team?.name ?? fallbackLabel}
      </Typography>
    </Stack>
  );

  const renderBoxScore = (
    team: ITeamMatchResponse | null,
    fallbackLabel: string,
    players: IPublicPlayerResponse[],
    form: Record<string, string>,
    setForm: React.Dispatch<React.SetStateAction<Record<string, string>>>
  ) => (
    <Card variant="outlined" sx={{ height: '100%' }}>
      <CardContent>
        <Typography variant="subtitle2" sx={{ color: 'text.secondary', mb: 1.5 }} noWrap>
          {team?.name ?? fallbackLabel}
        </Typography>

        {players.length === 0 ? (
          <Stack spacing={1} sx={{ alignItems: 'flex-start' }}>
            <Typography variant="body2" sx={{ color: 'text.secondary' }}>
              Sin jugadores registrados. No se puede cargar un resultado hasta
              que el equipo tenga jugadores en el plantel.
            </Typography>
            {team?.id && (
              <Button
                component={Link}
                to={APP_ROUTES.panelTeamDetail.build(team.id)}
                size="small"
                variant="outlined"
              >
                Ver plantel
              </Button>
            )}
          </Stack>
        ) : (
          <Stack spacing={0.5}>
            {players.map(player => (
              <Stack
                key={player.id}
                direction="row"
                spacing={1.25}
                sx={{ alignItems: 'center', py: 0.5 }}
              >
                <JerseySvg
                  color={team?.shirtColor}
                  secondaryColor={team?.shirtSecondaryColor}
                  style={team?.jerseyStyle}
                  number={player.jerseyNumber ?? undefined}
                  size={28}
                  title={`Camiseta de ${player.fullName}`}
                />
                <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center', flex: 1, minWidth: 0 }}>
                  <Typography variant="body2" noWrap>
                    {player.fullName}
                  </Typography>
                  {!resolveIsHabilitado(player.isHabilitado, player.medicalRecordStatus) && (
                    <HabilitacionBadge
                      isHabilitado={player.isHabilitado}
                      status={player.medicalRecordStatus}
                    />
                  )}
                </Stack>
                <TextField
                  type="number"
                  size="small"
                  variant="standard"
                  value={form[player.id] ?? '0'}
                  onChange={e =>
                    setForm(prev => ({
                      ...prev,
                      [player.id]: e.target.value,
                    }))
                  }
                  slotProps={{
                    htmlInput: {
                      min: 0,
                      style: { width: 32, textAlign: 'center', fontWeight: 700 },
                      'aria-label': `Puntos de ${player.fullName}`,
                    },
                  }}
                />
              </Stack>
            ))}
          </Stack>
        )}
      </CardContent>
    </Card>
  );

  if (loading) {
    return <TableSkeleton rows={5} columns={5} />;
  }

  return (
    <Stack spacing={3}>
      <Typography variant="body2" sx={{ color: 'text.secondary' }}>
        El resultado se calcula sumando los puntos de cada jugador. Cargá la
        planilla de ambos equipos y guardá para finalizar el partido.
      </Typography>

      {/* Live scoreboard: same crest-vs-crest, big-Oswald-score language as
          the public match page, reading the currently-typed sums instead of
          a recorded result. */}
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={{ xs: 2, sm: 3 }}
        sx={{ alignItems: 'center', justifyContent: 'center' }}
      >
        {renderTeamHeader(match.homeTeam, 'Equipo local', emphasis.home)}

        <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', flexShrink: 0 }}>
          {renderScoreNumber(homeSum, emphasis.home)}
          <Typography
            component="span"
            aria-hidden
            sx={{ fontFamily: font.display, fontWeight: 300, fontSize: { xs: '1.75rem', md: '2rem' }, color: 'text.secondary' }}
          >
            :
          </Typography>
          {renderScoreNumber(visitorSum, emphasis.visitor)}
        </Stack>

        {renderTeamHeader(match.visitorTeam, 'Equipo visitante', emphasis.visitor)}
      </Stack>

      <Box
        sx={{
          display: 'grid',
          gap: 2,
          gridTemplateColumns: { xs: '1fr', md: 'repeat(2, 1fr)' },
        }}
      >
        {renderBoxScore(match.homeTeam, 'Equipo local', homePlayers, homeForm, setHomeForm)}
        {renderBoxScore(match.visitorTeam, 'Equipo visitante', visitorPlayers, visitorForm, setVisitorForm)}
      </Box>

      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={2}
        sx={{ justifyContent: 'flex-end', alignItems: { sm: 'center' } }}
      >
        {isTie && (
          <Typography variant="body2" color="error">
            No se permiten empates: el partido debe tener un ganador.
          </Typography>
        )}
        {!rostersReady && (
          <Typography variant="body2" color="error">
            Ambos equipos necesitan jugadores en el plantel para cargar el
            resultado.
          </Typography>
        )}
        <Button
          variant="contained"
          onClick={() => void handleSaveResult()}
          disabled={submitting || isTie || !rostersReady}
        >
          Guardar resultado
        </Button>
      </Stack>
    </Stack>
  );
}
