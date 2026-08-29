import { useEffect, useRef, useState } from 'react';
import {
  Box,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import PageShell from '@/views/core/components/PageShell';
import SectionHeading from '@/views/core/components/SectionHeading';
import TeamLogo from '@/views/core/components/TeamLogo';
import CategoryChip from '@/views/core/components/CategoryChip';
import { TableSkeleton } from '@/views/core/components/skeletons';
import { championService } from '@/modules/champion/service/champion.service';
import { IChampionHistory } from '@/modules/champion/type/champion.d';

/**
 * Groups the flat champion history into ordered season buckets, preserving the
 * backend's ordering (the first time a season appears fixes its position).
 */
const groupBySeason = (
  history: IChampionHistory[]
): { seasonName: string; entries: IChampionHistory[] }[] => {
  const order: string[] = [];
  const bySeason = new Map<string, IChampionHistory[]>();

  history.forEach(entry => {
    // A tournament may not be assigned to a season yet — group those together
    // under a clear label instead of an empty heading.
    const key = entry.seasonName || 'Sin temporada';
    const existing = bySeason.get(key);
    if (existing) {
      existing.push(entry);
    } else {
      order.push(key);
      bySeason.set(key, [entry]);
    }
  });

  return order.map(seasonName => ({
    seasonName,
    entries: bySeason.get(seasonName) ?? [],
  }));
};

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

  const seasons = groupBySeason(history);

  return (
    <PageShell title="Campeones">

      {seasons.length === 0 ? (
        <Typography sx={{ color: 'text.secondary' }}>
          Todavía no hay campeones — se coronan al finalizar los torneos.
        </Typography>
      ) : (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
          {seasons.map(({ seasonName, entries }) => (
            <Box component="section" key={seasonName}>
              <SectionHeading>{seasonName}</SectionHeading>
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Torneo</TableCell>
                      <TableCell>Categoría</TableCell>
                      <TableCell>División</TableCell>
                      <TableCell>Campeón</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {entries.map(entry => (
                      <TableRow
                        key={`${entry.tournamentId}-${entry.divisionName}`}
                        hover
                      >
                        <TableCell>{entry.tournamentName}</TableCell>
                        <TableCell>
                          <CategoryChip category={entry.category} />
                        </TableCell>
                        <TableCell>{entry.divisionName}</TableCell>
                        <TableCell>
                          <Box
                            sx={{
                              display: 'flex',
                              alignItems: 'center',
                              gap: 1,
                            }}
                          >
                            <TeamLogo
                              teamName={entry.championTeam.teamName}
                              logoUrl={entry.championTeam.logoUrl}
                              size={28}
                            />
                            <Typography component="span">
                              {entry.championTeam.teamName}
                            </Typography>
                          </Box>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            </Box>
          ))}
        </Box>
      )}
    </PageShell>
  );
}
