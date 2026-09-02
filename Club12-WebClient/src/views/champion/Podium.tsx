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

/**
 * Every rank's grid column position on wide screens — 2nd on the left, 1st
 * in the middle, 3rd on the right — independent of DOM order, which stays
 * 1st/2nd/3rd (so the champion is still announced first by assistive tech
 * and on the mobile stacked layout, where `order` resets to plain rank).
 * The "no 3rd place" spacer below reuses column 3 so the champion's column
 * stays the row's true middle even with only two cards — centering the
 * PAIR as a unit instead would leave the champion off to one side.
 */
const GRID_COLUMN: Record<1 | 2 | 3, number> = { 2: 1, 1: 2, 3: 3 };

interface PodiumPlaceProps {
  rank: 1 | 2 | 3;
  team: IPodiumTeam | null;
}

function PodiumPlace({ rank, team }: PodiumPlaceProps) {
  const accent = PLACE_ACCENT[rank];
  const isChampion = rank === 1;
  const logoSize = isChampion ? 88 : 64;

  return (
    <Paper
      elevation={isChampion ? 6 : 2}
      sx={{
        order: { xs: rank, md: GRID_COLUMN[rank] },
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
 * A division's top-three podium (1º/2º/3º): three cards side by side — 2nd,
 * 1st, 3rd — with the champion largest, elevated and trophy-accented, always
 * in the middle column regardless of whether there are two or three decided
 * places. With only two places, the 3rd column is held open by an invisible
 * spacer instead of being omitted — omitting it would let the two remaining
 * cards center as a PAIR, leaving the champion off to one side of the row's
 * actual center rather than in it. Undecided places show a muted
 * "A definir" placeholder. Works whether the podium came from a playoff
 * bracket or straight from the final standings.
 */
export default function Podium({ podium }: PodiumProps) {
  // A playoff only has a 3rd place when its bracket includes a third-place
  // match. When it does not, `third` is null and the slot is held open by
  // an invisible spacer (see GRID_COLUMN) rather than showing a permanent
  // "A definir". A standings podium always keeps the three places (its
  // third comes straight from the table).
  const showThird = !podium.hasPlayoff || podium.third != null;

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
        {showThird ? (
          <PodiumPlace rank={3} team={podium.third} />
        ) : (
          <Box
            aria-hidden
            sx={{ display: { xs: 'none', md: 'block' }, order: GRID_COLUMN[3], flex: '1 1 0', maxWidth: 240 }}
          />
        )}
      </Box>
    </Box>
  );
}
