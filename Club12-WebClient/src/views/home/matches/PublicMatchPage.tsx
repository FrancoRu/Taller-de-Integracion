import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Box,
  Button,
  Card,
  CardContent,
  Divider,
  Stack,
  Typography,
} from '@mui/material';
import { useMatch } from '@/modules/match/hook/match.hook';
import TeamLogo from '@/views/core/components/TeamLogo';
import JerseySvg from '@/views/core/components/JerseySvg';
import MatchStatusChip from '@/views/match/MatchStatusChip';
import PageShell from '@/views/core/components/PageShell';
import SectionHeading from '@/views/core/components/SectionHeading';
import { DetailSkeleton } from '@/views/core/components/skeletons';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import {
  getScoreboardEmphasis,
  ScoreEmphasis,
  sortScorersByPoints,
} from '@/modules/match/utils/matchDisplay';
import { formatLongDateAr, formatTimeAr } from '@/modules/core/utils/formatDate';
import { ITeamMatchResponse } from '@/modules/team/type/team';
import { font } from '@/design/tokens';
import {
  DEFAULT_PAGE_METADATA,
  usePageMetadata,
} from '@/modules/core/utils/pageMetadata';
import { AccessTimeIcon, CalendarMonthIcon, StadiumIcon } from '@/views/core/MUI/icons/icons';

/** Both crests read at the same size — the league plays on neutral venues, so
 *  no side is presented as home/away. */
const CREST_SIZE = 88;

/** Accessible visually-hidden style (screen-reader-only). */
const visuallyHidden = {
  position: 'absolute',
  width: 1,
  height: 1,
  padding: 0,
  margin: -1,
  overflow: 'hidden',
  clip: 'rect(0 0 0 0)',
  whiteSpace: 'nowrap',
  border: 0,
} as const;

/** The score colour for each emphasis: the winner glows in the brand accent,
 *  the loser dims back, a neutral (not-yet-played) score reads as plain ink. */
const scoreColor: Record<ScoreEmphasis, string> = {
  winner: 'primary.main',
  loser: 'text.disabled',
  neutral: 'text.primary',
};

export default function PublicMatchPage() {
  const { matchId } = useParams<{ matchId: string }>();
  const navigate = useNavigate();
  const { match, getMatchById } = useMatch();
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!matchId) return;
    const fetch = async () => {
      setLoading(true);
      await getMatchById(matchId);
      setLoading(false);
    };
    void fetch();
  }, [matchId, getMatchById]);

  // Real back navigation, not a reconstructed tournament URL — the reader
  // got here from a specific zone/sub-tab/fecha, and a bare tournament link
  // always lands on its default tab, silently dropping that state. Only
  // falls back to a computed route when the match truly doesn't exist (the
  // not-found branch below), since there's nothing meaningful to go back to.
  const backRoute = match?.tournamentId
    ? APP_ROUTES.publicTournament.build(match.tournamentId)
    : APP_ROUTES.publicSeasons;
  const backLabel = match?.tournamentId ? 'Volver al torneo' : 'Volver a temporadas';
  const goBack = () => navigate(-1);
  const goToTournaments = () => navigate(backRoute);

  const matchup =
    match?.homeTeam?.name && match?.visitorTeam?.name
      ? `${match.homeTeam.name} vs ${match.visitorTeam.name}`
      : undefined;

  // Set the social/SEO title from the matchup once it resolves; the hook keeps
  // the site defaults while it is still undefined.
  usePageMetadata({
    ...DEFAULT_PAGE_METADATA,
    title: matchup,
    description: matchup
      ? `${matchup} · resultado y detalle del partido en la liga Club 12.`
      : undefined,
  });

  if (loading) {
    return (
      <PageShell maxWidth="md">
        <DetailSkeleton />
      </PageShell>
    );
  }

  if (!match || (match.id !== matchId && match.slug !== matchId)) {
    return (
      <PageShell maxWidth="md">
        <Typography variant="h5" component="h1" sx={{ mb: 2 }}>
          Partido no encontrado
        </Typography>
        <Typography sx={{ color: 'text.secondary', mb: 3 }}>
          El partido que buscás no existe o ya no está disponible.
        </Typography>
        <Button onClick={goToTournaments}>{backLabel}</Button>
      </PageShell>
    );
  }

  const { homeTeam, visitorTeam, isFinished, venue } = match;

  const emphasis = getScoreboardEmphasis({
    isFinished,
    homeTeamId: homeTeam?.id,
    visitorTeamId: visitorTeam?.id,
    winningTeamId: match.winningTeamId,
  });

  const renderTeam = (
    team: ITeamMatchResponse | null,
    side: ScoreEmphasis
  ) => (
    <Stack
      spacing={1.25}
      sx={{ alignItems: 'center', flex: 1, minWidth: 0, textAlign: 'center' }}
    >
      <TeamLogo
        teamName={team?.name ?? '—'}
        logoUrl={team?.logoUrl}
        size={CREST_SIZE}
      />
      <Typography
        variant="h6"
        component="p"
        sx={{
          fontWeight: side === 'winner' ? 700 : 600,
          color: side === 'loser' ? 'text.secondary' : 'text.primary',
          lineHeight: 1.2,
        }}
      >
        {team?.name ?? '—'}
      </Typography>
    </Stack>
  );

  const renderScoreNumber = (value: number, side: ScoreEmphasis) => (
    <Typography
      component="span"
      sx={{
        fontFamily: font.display,
        fontWeight: 700,
        fontSize: { xs: '3.25rem', md: '4.75rem' },
        lineHeight: 1,
        color: scoreColor[side],
      }}
    >
      {value}
    </Typography>
  );

  const renderScorers = (team: ITeamMatchResponse | null) => {
    const scorers = sortScorersByPoints(team?.scorers ?? []);

    return (
      <Card variant="outlined" sx={{ height: '100%' }}>
        <CardContent>
          <Typography
            variant="subtitle1"
            sx={{ fontWeight: 700, minWidth: 0, mb: 1.5 }}
            noWrap
          >
            {team?.name ?? '—'}
          </Typography>

          {scorers.length === 0 ? (
            <Typography variant="body2" sx={{ color: 'text.secondary' }}>
              Sin goleadores cargados.
            </Typography>
          ) : (
            <Stack component="ul" spacing={1} sx={{ listStyle: 'none', p: 0, m: 0 }}>
              {scorers.map(scorer => (
                <Stack
                  key={scorer.playerId}
                  component="li"
                  direction="row"
                  spacing={1.25}
                  sx={{ alignItems: 'center' }}
                >
                  <JerseySvg
                    color={team?.shirtColor}
                    secondaryColor={team?.shirtSecondaryColor}
                    tertiaryColor={team?.shirtTertiaryColor}
                    style={team?.jerseyStyle}
                    number={scorer.jerseyNumber ?? undefined}
                    size={30}
                    title={`Camiseta de ${scorer.fullName}`}
                  />
                  <Typography variant="body2" sx={{ minWidth: 0, flex: 1 }} noWrap>
                    {scorer.fullName}
                  </Typography>
                  <Typography
                    variant="body2"
                    sx={{ fontWeight: 700, flexShrink: 0, color: 'text.primary' }}
                  >
                    {scorer.points}
                  </Typography>
                </Stack>
              ))}
            </Stack>
          )}
        </CardContent>
      </Card>
    );
  };

  return (
    <PageShell
      maxWidth="md"
      back={{ label: backLabel, onClick: goBack }}
    >
      {/* The matchup is the page's heading; kept visually hidden because the
          design leads with the centred crest-vs-crest scoreboard instead. */}
      <Typography variant="h4" component="h1" sx={visuallyHidden}>
        {homeTeam?.name ?? '—'} vs {visitorTeam?.name ?? '—'}
      </Typography>

      {/* Date, time and venue always read the same way regardless of match
          status (previously the date/time swapped between a bold h6 and a
          plain caption depending on isFinished, so a scheduled match's
          header looked like a different component from a played one) — one
          consistent metadata row, icon-led like the rest of the app
          (MatchRow, MatchFixtureList's bye row). */}
      <Stack sx={{ alignItems: 'center', mb: { xs: 3, md: 4 } }} spacing={1}>
        <MatchStatusChip status={match.status} isFinished={isFinished} />
        <Stack
          direction="row"
          spacing={1.5}
          sx={{ alignItems: 'center', justifyContent: 'center', flexWrap: 'wrap', rowGap: 0.5 }}
        >
          <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
            <CalendarMonthIcon sx={{ fontSize: 18, color: 'text.secondary' }} />
            <Typography variant="body2" sx={{ color: 'text.secondary', fontWeight: 500 }}>
              {formatLongDateAr(match.matchDate)}
            </Typography>
          </Stack>
          <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
            <AccessTimeIcon sx={{ fontSize: 18, color: 'text.secondary' }} />
            <Typography variant="body2" sx={{ color: 'text.secondary', fontWeight: 500 }}>
              {formatTimeAr(match.matchDate)}
            </Typography>
          </Stack>
        </Stack>
        <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
          <StadiumIcon sx={{ fontSize: 18, color: 'text.secondary' }} />
          <Typography variant="body2" sx={{ color: 'text.secondary' }}>
            {venue?.name ?? 'Cancha a confirmar'}
          </Typography>
        </Stack>
      </Stack>

      {/* Scoreboard: teams flank a big centred score on desktop and stack above
          and below it on mobile. Same DOM order in both, so the score always
          sits between the two teams. */}
      <Stack
        direction={{ xs: 'column', md: 'row' }}
        spacing={{ xs: 2.5, md: 3 }}
        sx={{ alignItems: 'center', justifyContent: 'center', mb: { xs: 3, md: 4 } }}
      >
        {renderTeam(homeTeam, emphasis.home)}

        <Stack
          direction="row"
          spacing={{ xs: 1.5, md: 2 }}
          sx={{ alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}
        >
          {isFinished ? (
            <>
              {renderScoreNumber(homeTeam?.score ?? 0, emphasis.home)}
              <Typography
                component="span"
                aria-hidden
                sx={{
                  fontFamily: font.display,
                  fontWeight: 300,
                  fontSize: { xs: '2.25rem', md: '3rem' },
                  lineHeight: 1,
                  color: 'text.secondary',
                }}
              >
                :
              </Typography>
              {renderScoreNumber(visitorTeam?.score ?? 0, emphasis.visitor)}
            </>
          ) : (
            <Typography
              component="span"
              sx={{
                fontFamily: font.display,
                fontWeight: 600,
                fontSize: { xs: '2.5rem', md: '3.25rem' },
                lineHeight: 1,
                color: 'text.secondary',
                letterSpacing: '0.08em',
              }}
            >
              VS
            </Typography>
          )}
        </Stack>

        {renderTeam(visitorTeam, emphasis.visitor)}
      </Stack>

      {isFinished && (
        <>
          <Divider sx={{ mb: 3 }} />

          <Box component="section">
            <SectionHeading>Goleadores del partido</SectionHeading>
            <Box
              sx={{
                display: 'grid',
                gap: 2,
                gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)' },
              }}
            >
              {renderScorers(homeTeam)}
              {renderScorers(visitorTeam)}
            </Box>
          </Box>
        </>
      )}
    </PageShell>
  );
}
