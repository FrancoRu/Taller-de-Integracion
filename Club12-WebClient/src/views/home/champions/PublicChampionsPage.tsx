import { useEffect, useRef, useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import {
  Box,
  Card,
  CardContent,
  Grid,
  Link,
  Typography,
} from '@mui/material';
import PageShell from '@/views/core/components/PageShell';
import SectionHeading from '@/views/core/components/SectionHeading';
import TeamLogo from '@/views/core/components/TeamLogo';
import CategoryChip from '@/views/core/components/CategoryChip';
import { EmojiEventsIcon } from '@/views/core/MUI/icons/icons';
import { TableSkeleton } from '@/views/core/components/skeletons';
import { championService } from '@/modules/champion/service/champion.service';
import { IChampionHistory } from '@/modules/champion/type/champion.d';
import { groupChampions } from '@/modules/champion/utils/groupChampions';
import { categoryColor } from '@/design/categoryColor';
import { hexToRgba } from '@/design/colorName';
import { brand, font } from '@/design/tokens';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';

/**
 * A single champion card: division/cup label, its tournament, and the crowned
 * team (crest + name) linking to the team's public page. Gold is the signature
 * accent — a trophy mark and a top border — because every card here is a title.
 */
function ChampionCard({ entry }: { entry: IChampionHistory }) {
  const { championTeam } = entry;

  return (
    <Card
      variant="outlined"
      sx={{
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
        overflow: 'hidden',
        borderColor: hexToRgba(brand.gold, 0.35),
        transition: 'transform 0.15s ease, box-shadow 0.15s ease, border-color 0.15s ease',
        '&:hover': {
          transform: 'translateY(-2px)',
          borderColor: brand.gold,
          boxShadow: `0 10px 28px -14px ${hexToRgba(brand.gold, 0.7)}`,
        },
      }}
    >
      {/* Gold "CAMPEÓN" banner — every card here is a title. */}
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          gap: 0.75,
          py: 0.75,
          background: `linear-gradient(90deg, ${brand.gold}, ${brand.goldLight}, ${brand.gold})`,
          color: brand.orangeInk,
        }}
      >
        <EmojiEventsIcon sx={{ fontSize: 18 }} />
        <Typography
          component="span"
          sx={{ fontWeight: 800, letterSpacing: '0.14em', fontSize: '0.72rem' }}
        >
          CAMPEÓN
        </Typography>
      </Box>

      <CardContent
        sx={{
          flexGrow: 1,
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          textAlign: 'center',
          gap: 1,
          pt: 2.5,
          pb: 2.5,
        }}
      >
        {/* Crowned crest: a gold ring over a soft gold glow. */}
        <Box sx={{ position: 'relative', display: 'inline-flex', mb: 0.5 }}>
          <Box
            aria-hidden
            sx={{
              position: 'absolute',
              inset: -10,
              borderRadius: '50%',
              background: `radial-gradient(circle, ${hexToRgba(brand.gold, 0.28)} 0%, transparent 70%)`,
            }}
          />
          <Box
            sx={{
              position: 'relative',
              borderRadius: '50%',
              p: '3px',
              border: `2px solid ${brand.gold}`,
            }}
          >
            <TeamLogo
              teamName={championTeam.teamName}
              logoUrl={championTeam.logoUrl}
              size={64}
            />
          </Box>
        </Box>

        <Link
          component={RouterLink}
          to={APP_ROUTES.publicTeam.build(championTeam.teamId)}
          underline="none"
          sx={{
            color: 'text.primary',
            transition: 'color 0.15s',
            '&:hover': { color: brand.goldLight },
            '&:focus-visible': {
              outline: '2px solid',
              outlineColor: brand.gold,
              outlineOffset: 3,
              borderRadius: 1,
            },
          }}
        >
          <Typography
            component="span"
            sx={{
              fontFamily: font.display,
              fontWeight: 700,
              fontSize: '1.15rem',
              lineHeight: 1.15,
              textTransform: 'uppercase',
              letterSpacing: '0.01em',
            }}
          >
            {championTeam.teamName}
          </Typography>
        </Link>

        <Typography variant="caption" sx={{ color: 'text.secondary' }}>
          {entry.divisionName} · {entry.tournamentName}
        </Typography>
      </CardContent>
    </Card>
  );
}

export default function PublicChampionsPage() {
  const [loading, setLoading] = useState(true);
  const [history, setHistory] = useState<IChampionHistory[]>([]);
  const getChampionsHistoryRef = useRef(championService.getChampionsHistory);

  useEffect(() => {
    let cancelled = false;

    const fetch = async () => {
      setLoading(true);
      try {
        const response = await getChampionsHistoryRef.current();
        if (!cancelled) setHistory(response.data ?? []);
      } catch {
        if (!cancelled) setHistory([]);
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    void fetch();
    return () => {
      cancelled = true;
    };
  }, []);

  if (loading) {
    return (
      <PageShell title="Campeones">
        <TableSkeleton rows={8} columns={4} />
      </PageShell>
    );
  }

  const seasons = groupChampions(history);

  return (
    <PageShell title="Campeones">
      {seasons.length === 0 ? (
        <Typography sx={{ color: 'text.secondary' }}>
          Todavía no hay campeones — se coronan al finalizar los torneos.
        </Typography>
      ) : (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 5 }}>
          {seasons.map(({ seasonName, categories }) => (
            <Box component="section" key={seasonName}>
              <SectionHeading component="h2" accentColor={brand.gold}>
                {seasonName}
              </SectionHeading>

              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
                {categories.map(({ category, entries }) => (
                  <Box component="section" key={category}>
                    <Box
                      sx={{
                        display: 'flex',
                        alignItems: 'center',
                        gap: 1.25,
                        mb: 1.5,
                        pb: 1,
                        borderBottom: '2px solid',
                        borderBottomColor: categoryColor(category).fill,
                      }}
                    >
                      <CategoryChip category={category} />
                    </Box>

                    <Grid container spacing={2}>
                      {entries.map(entry => (
                        <Grid
                          key={`${entry.tournamentId}-${entry.divisionName}`}
                          size={{ xs: 12, sm: 6, md: 4 }}
                        >
                          <ChampionCard entry={entry} />
                        </Grid>
                      ))}
                    </Grid>
                  </Box>
                ))}
              </Box>
            </Box>
          ))}
        </Box>
      )}
    </PageShell>
  );
}
