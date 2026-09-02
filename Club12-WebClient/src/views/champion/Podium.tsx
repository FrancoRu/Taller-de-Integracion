import { Box, Paper, Typography } from '@mui/material';
import TeamLogo from '@/views/core/components/TeamLogo';
import { EmojiEventsIcon } from '@/views/core/MUI/icons/icons';
import { IPodium, IPodiumTeam } from '@/modules/champion/type/champion.d';
import { brand } from '@/design/tokens';

/**
 * Subtle medal accents for each podium place. Gold is the club's championship
 * gold from the brand; silver/bronze are neutral metallics that read on the
 * dark canvas.
 */
const PLACE_ACCENT: Record<1 | 2 | 3, string> = {
  1: brand.gold,
  2: '#C7CDD6',
  3: '#CD8E5A',
};

const PLACE_LABEL: Record<1 | 2 | 3, string> = {
  1: '1º',
  2: '2º',
  3: '3º',
};

interface PodiumPlaceProps {
  rank: 1 | 2 | 3;
  team: IPodiumTeam | null;
  /**
   * Set when this is the only flex child in its row (the 2-place layout,
   * where the champion is alone with no 2nd/3rd to flank it). The classic
   * 2-1-3 `order` below only makes sense relative to siblings at the other
   * ranks — left on a lone item it still applies (order is compared against
   * every flex item's default of 0), silently sorting the "champion" after
   * a later sibling with no order override of its own.
   */
  standalone?: boolean;
}

function PodiumPlace({ rank, team, standalone = false }: PodiumPlaceProps) {
  const accent = PLACE_ACCENT[rank];
  const isChampion = rank === 1;
  const logoSize = isChampion ? 88 : 64;

  return (
    <Paper
      elevation={isChampion ? 6 : 2}
      sx={{
        // Classic podium ordering: 2 – 1 – 3 on wide screens, 1 – 2 – 3 stacked
        // on mobile so the champion always reads first when scrolling.
        order: standalone ? 'unset' : { xs: rank, md: rank === 1 ? 2 : rank === 2 ? 1 : 3 },
        flex: { xs: '1 1 100%', md: '1 1 0' },
        maxWidth: { md: 240 },
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        gap: 1,
        px: 2,
        pt: isChampion ? 3 : 2.5,
        pb: isChampion ? 3.5 : 2.5,
        borderRadius: 2,
        borderTop: `4px solid ${accent}`,
        // Lift the champion so the three blocks form a podium silhouette.
        mt: { md: isChampion ? 0 : 3 },
        textAlign: 'center',
      }}
    >
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: 0.5,
          color: accent,
          fontWeight: 700,
        }}
      >
        {isChampion && <EmojiEventsIcon fontSize="small" />}
        <Typography component="span" variant="subtitle2" sx={{ fontWeight: 700 }}>
          {PLACE_LABEL[rank]}
        </Typography>
      </Box>

      {team ? (
        <>
          <TeamLogo teamName={team.teamName} logoUrl={team.logoUrl} size={logoSize} />
          <Typography
            variant={isChampion ? 'subtitle1' : 'body2'}
            sx={{ fontWeight: isChampion ? 700 : 500, lineHeight: 1.2 }}
          >
            {team.teamName}
          </Typography>
        </>
      ) : (
        <>
          <Box
            aria-hidden
            sx={{
              width: logoSize,
              height: logoSize,
              borderRadius: '50%',
              border: '2px dashed',
              borderColor: 'divider',
            }}
          />
          <Typography variant="body2" sx={{ color: 'text.secondary' }}>
            A definir
          </Typography>
        </>
      )}
    </Paper>
  );
}

interface PodiumProps {
  podium: IPodium;
}

/**
 * A division's top-three podium (1º/2º/3º): the champion is largest, centered
 * and trophy-accented, with runner-up and third beside it (or below on mobile).
 * Undecided places show a muted "A definir" placeholder. Works whether the
 * podium came from a playoff bracket or straight from the final standings.
 */
export default function Podium({ podium }: PodiumProps) {
  // A playoff only has a 3rd place when its bracket includes a third-place
  // match. When it does not, `third` is null and we omit the slot entirely
  // rather than showing a permanent "A definir". A standings podium always
  // keeps the three places (its third comes straight from the table).
  const showThird = !podium.hasPlayoff || podium.third != null;

  if (!showThird) {
    // The classic 2-1-3 arrangement centers the CHAMPION by flanking it with
    // runners-up on both sides. With only two places decided there's nothing
    // to put on the champion's other side, so the pair as a whole reads
    // centered on the page while the champion itself — the actual focal
    // point — sits off to one side of it. Center the champion alone instead,
    // with the runner-up as its own smaller card underneath (not a bare
    // text line — a lone line looked unfinished next to the full card above
    // it, not like a deliberate secondary place).
    return (
      <Box
        component="section"
        aria-label={`Podio de ${podium.divisionName}`}
        sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 2 }}
      >
        <PodiumPlace rank={1} team={podium.first} standalone />
        <Paper
          variant="outlined"
          sx={{
            display: 'flex',
            alignItems: 'center',
            gap: 1.5,
            px: 2.5,
            py: 1.5,
            borderRadius: 2,
            borderTop: `3px solid ${PLACE_ACCENT[2]}`,
          }}
        >
          <Typography
            component="span"
            variant="subtitle2"
            sx={{ color: PLACE_ACCENT[2], fontWeight: 700 }}
          >
            {PLACE_LABEL[2]}
          </Typography>
          {podium.second ? (
            <>
              <TeamLogo teamName={podium.second.teamName} logoUrl={podium.second.logoUrl} size={36} />
              <Typography variant="body1" sx={{ fontWeight: 600 }}>
                {podium.second.teamName}
              </Typography>
            </>
          ) : (
            <Typography variant="body2" sx={{ color: 'text.secondary' }}>
              A definir
            </Typography>
          )}
        </Paper>
      </Box>
    );
  }

  return (
    <Box component="section" aria-label={`Podio de ${podium.divisionName}`}>
      <Box
        sx={{
          display: 'flex',
          flexWrap: 'wrap',
          alignItems: { md: 'flex-end' },
          justifyContent: 'center',
          gap: 2,
        }}
      >
        <PodiumPlace rank={1} team={podium.first} />
        <PodiumPlace rank={2} team={podium.second} />
        <PodiumPlace rank={3} team={podium.third} />
      </Box>
    </Box>
  );
}
