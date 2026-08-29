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
}

function PodiumPlace({ rank, team }: PodiumPlaceProps) {
  const accent = PLACE_ACCENT[rank];
  const isChampion = rank === 1;
  const logoSize = isChampion ? 88 : 64;

  return (
    <Paper
      elevation={isChampion ? 6 : 2}
      sx={{
        // Classic podium ordering: 2 – 1 – 3 on wide screens, 1 – 2 – 3 stacked
        // on mobile so the champion always reads first when scrolling.
        order: { xs: rank, md: rank === 1 ? 2 : rank === 2 ? 1 : 3 },
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
