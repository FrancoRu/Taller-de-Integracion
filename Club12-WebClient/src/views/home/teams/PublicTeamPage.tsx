import { useEffect, useState } from 'react';
import { Link as RouterLink, useNavigate, useParams } from 'react-router-dom';
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  Container,
  Divider,
  FormControl,
  Grid,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  SelectChangeEvent,
  Stack,
  Typography,
} from '@mui/material';
import { useTeam } from '@/modules/team/hook/team.hook';
import {
  useTeamParticipations,
  useTeamScorers,
  useTeamStandings,
  useTeamTitles,
} from '@/modules/team/hook/useTeamProfile';
import {
  computeRecord,
  deriveStreak,
  formatDifferential,
  formatPosition,
  formatRecord,
  splitFixture,
  TeamRecord,
} from '@/modules/team/utils/teamProfile';
import {
  TeamMatch,
  TeamMatchResult,
  TeamParticipation,
  TeamSummary,
} from '@/modules/team/type/teamProfile.d';
import { IScorerByPlayerResponse } from '@/modules/scorer/type/scorer.d';
import { IChampionHistory } from '@/modules/champion/type/champion.d';
import { GUID } from '@/modules/core/types/types';
import { formatDateTimeAr } from '@/modules/core/utils/formatDate';
import { brand } from '@/design/tokens';
import TeamHero from '@/views/core/components/TeamHero';
import TeamBackdrop from '@/views/core/components/TeamBackdrop';
import TeamLogo from '@/views/core/components/TeamLogo';
import JerseySvg from '@/views/core/components/JerseySvg';
import SectionHeading from '@/views/core/components/SectionHeading';
import CategoryChip from '@/views/core/components/CategoryChip';
import StatTile from '@/views/core/components/StatTile';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';

/** A participation's option label, e.g. "Apertura 2025 · Temporada 2025". */
const participationLabel = (participation: TeamParticipation): string =>
  participation.seasonName
    ? `${participation.tournamentName} · ${participation.seasonName}`
    : participation.tournamentName;

/**
 * The season/tournament selector. Renders a compact Select when the team has
 * played more than one tournament; a static label otherwise. Always shows the
 * active tournament's category chip.
 */
function SeasonSelector({
  participations,
  activeTournamentId,
  onChange,
}: {
  participations: TeamParticipation[];
  activeTournamentId: GUID | undefined;
  onChange: (tournamentId: GUID) => void;
}) {
  const active = participations.find(
    participation => participation.tournamentId === activeTournamentId
  );

  if (participations.length === 0) return null;

  const handleChange = (event: SelectChangeEvent) =>
    onChange(event.target.value as GUID);

  return (
    <Stack
      direction="row"
      spacing={1.5}
      sx={{ alignItems: 'center', flexWrap: 'wrap', gap: 1 }}
    >
      {participations.length > 1 ? (
        <FormControl size="small" sx={{ minWidth: 220 }}>
          <InputLabel id="team-tournament-label">Torneo</InputLabel>
          <Select
            labelId="team-tournament-label"
            label="Torneo"
            value={activeTournamentId ?? ''}
            onChange={handleChange}
          >
            {participations.map(participation => (
              <MenuItem
                key={participation.tournamentId}
                value={participation.tournamentId}
              >
                {participationLabel(participation)}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      ) : (
        active && (
          <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
            {participationLabel(active)}
          </Typography>
        )
      )}
      {active && <CategoryChip category={active.category} />}
    </Stack>
  );
}

/**
 * The box-score stat row. Position comes from the group-stage standing (a
 * zone-relative concept), while record, points and differential are aggregated
 * from ALL the team's finished matches so they stay consistent with the streak
 * and fixture below — and so a playoff-only team (no standing) still shows a
 * record. Falls back to a quiet empty state when there is nothing to show.
 */
function ResumenBlock({
  summary,
  record,
}: {
  summary: TeamSummary | null;
  record: TeamRecord;
}) {
  if (!summary && record.played === 0) {
    return (
      <Typography sx={{ color: 'text.secondary' }}>
        Sin datos para este torneo todavía.
      </Typography>
    );
  }

  const differentialTone = record.pointsDifference >= 0 ? 'positive' : 'negative';

  return (
    <Box
      sx={{
        display: 'flex',
        flexWrap: 'wrap',
        gap: 1.5,
      }}
    >
      {summary && (
        <StatTile
          label="Posición"
          value={formatPosition(summary.position)}
          sub={`de ${summary.totalTeams} · ${summary.divisionName}`}
          tone="accent"
        />
      )}
      <StatTile
        label="Record"
        value={formatRecord(record.wins, record.losses)}
        sub={`${record.played} jugados`}
      />
      <StatTile
        label="Diferencial"
        value={formatDifferential(record.pointsDifference)}
        tone={differentialTone}
      />
      <StatTile label="PF" value={record.pointsFor} sub="a favor" />
      <StatTile label="PC" value={record.pointsAgainst} sub="en contra" />
    </Box>
  );
}

/** The last-5 form as a row of W/L pills. */
function StreakRow({ streak }: { streak: TeamMatchResult[] }) {
  if (streak.length === 0) return null;

  return (
    <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center' }}>
      {streak.map((result, index) => {
        const won = result === 'W';
        return (
          <Box
            key={`${index}-${result}`}
            aria-label={won ? 'Victoria' : 'Derrota'}
            sx={{
              width: 26,
              height: 26,
              borderRadius: '50%',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontWeight: 700,
              fontSize: '0.8rem',
              color: '#fff',
              bgcolor: won ? 'success.main' : 'error.main',
            }}
          >
            {result}
          </Box>
        );
      })}
    </Stack>
  );
}

/** A single compact, team-oriented fixture row. */
function TeamMatchRow({ match }: { match: TeamMatch }) {
  const finished = match.isFinished;
  const teamWon = match.result === 'W';

  return (
    <Box
      component={RouterLink}
      to={APP_ROUTES.publicMatch.build(match.matchId)}
      sx={{
        // The league is neutral-venue (courts are rented per tournament), so
        // there is no home/away distinction to surface — just opponent, score
        // and date. `isHome` still exists on the model but is not shown.
        display: 'grid',
        gridTemplateColumns: { xs: '1fr auto', sm: '1fr auto 132px' },
        alignItems: 'center',
        gap: { xs: 1, sm: 2 },
        px: 2,
        py: 1.25,
        textDecoration: 'none',
        color: 'inherit',
        transition: 'background-color 0.15s ease',
        '&:hover': { bgcolor: 'action.hover' },
      }}
    >
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center', minWidth: 0 }}>
        <TeamLogo
          teamName={match.opponentName}
          logoUrl={match.opponentLogoUrl}
          size={28}
        />
        <Typography variant="body2" noWrap sx={{ fontWeight: 500, minWidth: 0 }}>
          {match.opponentName}
        </Typography>
      </Stack>

      <Box sx={{ textAlign: 'center', minWidth: 56 }}>
        {finished && match.teamScore !== null && match.opponentScore !== null ? (
          <Typography variant="body1" component="span">
            <Box
              component="span"
              sx={{ fontWeight: teamWon ? 700 : 400 }}
            >
              {match.teamScore}
            </Box>
            {' - '}
            <Box
              component="span"
              sx={{ fontWeight: !teamWon ? 700 : 400 }}
            >
              {match.opponentScore}
            </Box>
          </Typography>
        ) : (
          <Typography variant="caption" sx={{ color: 'text.secondary' }}>
            {formatDateTimeAr(match.matchDate)}
          </Typography>
        )}
      </Box>

      <Typography
        variant="caption"
        noWrap
        sx={{
          color: 'text.secondary',
          textAlign: 'right',
          display: { xs: 'none', sm: 'block' },
        }}
      >
        {finished ? formatDateTimeAr(match.matchDate) : (match.venueName ?? '')}
      </Typography>
    </Box>
  );
}

/** "Próximos" and "Últimos" fixture lists, or a quiet empty state. */
function FixtureBlock({ matches }: { matches: TeamMatch[] }) {
  const { upcoming, recent } = splitFixture(matches);

  if (upcoming.length === 0 && recent.length === 0) {
    return (
      <Typography sx={{ color: 'text.secondary' }}>
        Sin partidos para este torneo todavía.
      </Typography>
    );
  }

  return (
    <Stack spacing={3}>
      {upcoming.length > 0 && (
        <Box>
          <Typography
            variant="overline"
            sx={{ color: 'text.secondary', display: 'block', mb: 1 }}
          >
            Próximos
          </Typography>
          <Paper variant="outlined">
            <Stack divider={<Divider />}>
              {upcoming.map(match => (
                <TeamMatchRow key={match.matchId} match={match} />
              ))}
            </Stack>
          </Paper>
        </Box>
      )}
      {recent.length > 0 && (
        <Box>
          <Typography
            variant="overline"
            sx={{ color: 'text.secondary', display: 'block', mb: 1 }}
          >
            Últimos
          </Typography>
          <Paper variant="outlined">
            <Stack divider={<Divider />}>
              {recent.map(match => (
                <TeamMatchRow key={match.matchId} match={match} />
              ))}
            </Stack>
          </Paper>
        </Box>
      )}
    </Stack>
  );
}

/** The team's top scorers, or a quiet empty state. */
function ScorersBlock({ scorers }: { scorers: IScorerByPlayerResponse[] }) {
  if (scorers.length === 0) {
    return (
      <Typography sx={{ color: 'text.secondary' }}>
        Sin goleadores registrados para este torneo.
      </Typography>
    );
  }

  return (
    <Paper variant="outlined">
      <Stack divider={<Divider />}>
        {scorers.map((scorer, index) => (
          <Stack
            key={scorer.playerId}
            direction="row"
            spacing={2}
            sx={{ alignItems: 'center', px: 2, py: 1.25 }}
          >
            <Typography
              variant="body2"
              sx={{ color: 'text.secondary', width: 20 }}
            >
              {index + 1}
            </Typography>
            <Typography variant="body2" noWrap sx={{ fontWeight: 500, flex: 1 }}>
              {scorer.fullName}
            </Typography>
            <Typography variant="body1" sx={{ fontWeight: 700 }}>
              {scorer.points}
            </Typography>
          </Stack>
        ))}
      </Stack>
    </Paper>
  );
}

/** The team's titles as gold-accented chips. Rendered only when there are any. */
function TitlesBlock({ titles }: { titles: IChampionHistory[] }) {
  if (titles.length === 0) return null;

  return (
    <Box component="section" sx={{ mt: 4 }}>
      <SectionHeading accentColor={brand.gold}>Títulos</SectionHeading>
      <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
        {titles.map(title => (
          <Chip
            key={`${title.tournamentId}-${title.divisionName}`}
            label={`${title.tournamentName} · ${title.divisionName}`}
            sx={{
              bgcolor: 'transparent',
              border: '1px solid',
              borderColor: brand.gold,
              color: brand.gold,
              fontWeight: 600,
            }}
          />
        ))}
      </Stack>
    </Box>
  );
}

export default function PublicTeamPage() {
  const { teamId } = useParams<{ teamId: string }>();
  const navigate = useNavigate();
  const { team, getTeamById } = useTeam();
  const [loading, setLoading] = useState(false);
  const [activeTournamentId, setActiveTournamentId] = useState<GUID | undefined>(
    undefined
  );

  useEffect(() => {
    if (!teamId) return;
    const fetch = async () => {
      setLoading(true);
      await getTeamById(teamId);
      setLoading(false);
    };
    void fetch();
  }, [teamId, getTeamById]);

  const { participations } = useTeamParticipations(teamId);

  // Default the selection to the ongoing tournament (falling back to the newest)
  // once participations load, and keep it valid if the list changes.
  useEffect(() => {
    if (participations.length === 0) return;
    setActiveTournamentId(prev => {
      if (prev && participations.some(p => p.tournamentId === prev)) return prev;
      const current =
        participations.find(p => p.isCurrent) ?? participations[0];
      return current.tournamentId;
    });
  }, [participations]);

  const { summary, matches } = useTeamStandings(teamId, activeTournamentId);
  const { scorers } = useTeamScorers(team?.id, activeTournamentId);
  const { titles } = useTeamTitles(team?.id);

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 10 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!team || (team.id !== teamId && team.slug !== teamId)) {
    return (
      <Container maxWidth="md" sx={{ py: 5 }}>
        <Typography variant="h5" component="h1" sx={{ mb: 2 }}>
          Equipo no encontrado
        </Typography>
        <Button onClick={() => navigate(APP_ROUTES.publicSeasons)}>
          Volver a temporadas
        </Button>
      </Container>
    );
  }

  const streak = deriveStreak(matches);
  const record = computeRecord(matches);

  return (
    <TeamBackdrop shirtColor={team.shirtColor} logoUrl={team.logoUrl}>
      <Container maxWidth="md" sx={{ py: 5 }}>
        <Button
          onClick={() => navigate(-1)}
          sx={{ mb: 3, pl: 0 }}
          color="inherit"
        >
          ← Volver
        </Button>

        <Box sx={{ mb: 3 }}>
          <TeamHero
            name={team.name}
            code={team.threeLetterCode}
            logoUrl={team.logoUrl}
            shirtColor={team.shirtColor}
            secondaryColor={team.shirtSecondaryColor}
            jerseyStyle={team.jerseyStyle}
          />
        </Box>

        {participations.length > 0 && (
          <Box sx={{ mb: 3 }}>
            <SeasonSelector
              participations={participations}
              activeTournamentId={activeTournamentId}
              onChange={setActiveTournamentId}
            />
          </Box>
        )}

        <Box component="section" sx={{ mb: 4 }}>
          <SectionHeading>Resumen</SectionHeading>
          <ResumenBlock summary={summary} record={record} />
        </Box>

        {streak.length > 0 && (
          <Box component="section" sx={{ mb: 4 }}>
            <SectionHeading>Racha</SectionHeading>
            <StreakRow streak={streak} />
          </Box>
        )}

        <Box component="section" sx={{ mb: 4 }}>
          <SectionHeading>Fixture</SectionHeading>
          <FixtureBlock matches={matches} />
        </Box>

        <Box component="section" sx={{ mb: 4 }}>
          <SectionHeading>Goleadores</SectionHeading>
          <ScorersBlock scorers={scorers} />
        </Box>

        <TitlesBlock titles={titles} />

        <Divider sx={{ my: 4 }} />

        <Box component="section">
          <SectionHeading>Plantel</SectionHeading>
          {!team.players || team.players.length === 0 ? (
            <Typography sx={{ color: 'text.secondary' }}>
              Este equipo no tiene jugadores registrados.
            </Typography>
          ) : (
            <Grid container spacing={1.5}>
              {team.players.map(player => (
                <Grid key={player.id} size={{ xs: 12, sm: 6 }}>
                  <Paper variant="outlined" sx={{ px: 2, py: 1.25 }}>
                    <Stack
                      direction="row"
                      spacing={2}
                      sx={{ alignItems: 'center' }}
                    >
                      <JerseySvg
                        color={team.shirtColor}
                        secondaryColor={team.shirtSecondaryColor}
                        style={team.jerseyStyle}
                        number={player.jerseyNumber}
                        size={28}
                        title={`Camiseta de ${player.fullName}`}
                      />
                      <Typography
                        variant="body2"
                        noWrap
                        sx={{ fontWeight: 500 }}
                      >
                        {player.fullName}
                      </Typography>
                    </Stack>
                  </Paper>
                </Grid>
              ))}
            </Grid>
          )}
        </Box>
      </Container>
    </TeamBackdrop>
  );
}
