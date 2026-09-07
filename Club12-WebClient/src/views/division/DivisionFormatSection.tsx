import { useEffect, useState } from 'react';
import { Alert, Box, Paper, Stack, Typography } from '@mui/material';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import {
  DivisionStructureSummary,
  findDivisionStructure,
} from '@/modules/division/utils/divisionStructureSummary';
import { CupConfig } from '@/views/tournament/wizard/types';
import {
  buildCrossCupNodes,
  buildGroupAndCupNodes,
  subGroupLetter,
} from '@/views/tournament/wizard/wizardLogic';
import TreeNodeList from '@/views/tournament/wizard/TreeNodeList';
import PlayoffBracket from '@/views/playoff/PlayoffBracket';
import { buildTemplateBracketModel } from '@/modules/playoff/templateBracket';
import { ArrowForwardIcon } from '@/views/core/MUI/icons/icons';
import { DetailSkeleton } from '@/views/core/components/skeletons';

interface DivisionFormatSectionProps {
  /** The parent tournament's GUID or slug, passed straight to `getStructure`. */
  tournamentIdOrSlug: string;
  /** This division's name — must match exactly how it's named in the tournament's structure tree. */
  divisionName: string;
}

/** Plain group-phase boxes (one per sub-group), rendered ahead of a cup's bracket. */
function GroupPhaseBoxes({ subGroupCount }: { subGroupCount: number }) {
  return (
    <Stack spacing={1}>
      {Array.from({ length: subGroupCount }, (_, index) => (
        <Paper
          key={index}
          variant="outlined"
          sx={{ px: 2, py: 1, minWidth: 160, textAlign: 'center' }}
        >
          <Typography variant="body2">
            {subGroupCount > 1 ? `Grupo ${subGroupLetter(index)}` : 'Fase de grupos'}
          </Typography>
        </Paper>
      ))}
    </Stack>
  );
}

/** One cup's bracket-shaped template preview, labelled with the cup's name. */
function CupBracketPreview({ cup }: { cup: CupConfig }) {
  return (
    <Box>
      <Typography variant="subtitle2" sx={{ mb: 1 }}>
        {cup.name || '(sin nombre)'}
      </Typography>
      <PlayoffBracket model={buildTemplateBracketModel(cup)} />
    </Box>
  );
}

/**
 * Reads a division's format straight out of its tournament's structure tree
 * (the same data the tournament-cloning wizard-prefill already reconstructs)
 * and shows it two ways: a plain-language tree (reusing the wizard's own
 * review-step rendering) and a bracket-shaped diagram of each cup — a
 * template preview built from empty placeholder slots, since a division's
 * FORMAT is meaningful before any team is ever drawn into it.
 */
export default function DivisionFormatSection({
  tournamentIdOrSlug,
  divisionName,
}: DivisionFormatSectionProps) {
  const { getStructure } = useTournament();
  const [summary, setSummary] = useState<DivisionStructureSummary | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;

    const fetchStructure = async () => {
      setLoading(true);
      const structure = await getStructure(tournamentIdOrSlug);
      if (!cancelled) {
        setSummary(structure ? findDivisionStructure(structure, divisionName) : null);
        setLoading(false);
      }
    };

    void fetchStructure();
    return () => {
      cancelled = true;
    };
  }, [getStructure, tournamentIdOrSlug, divisionName]);

  if (loading) {
    return <DetailSkeleton />;
  }

  if (!summary) {
    return (
      <Typography variant="body2" sx={{ color: 'text.secondary' }}>
        No fue posible determinar el formato de esta división.
      </Typography>
    );
  }

  const hasGroupStage = summary.zone ? summary.zone.hasGroupStage : true;
  const subGroupCount = summary.zone ? summary.zone.subGroupCount : summary.crossCup!.groupCount;
  const cups = summary.zone ? summary.zone.cups : summary.crossCup!.cups;

  const textNodes = summary.zone
    ? buildGroupAndCupNodes(
        summary.zone.id,
        summary.zone.hasGroupStage,
        summary.zone.roundRobinLegs,
        summary.zone.subGroupCount,
        summary.zone.cups
      )
    : buildCrossCupNodes(summary.crossCup!);

  return (
    <Stack spacing={4}>
      {summary.review.length > 0 && (
        <Alert severity="warning">
          <Stack spacing={0.5}>
            {summary.review.map(notice => (
              <Typography key={notice} variant="body2">
                {notice}
              </Typography>
            ))}
          </Stack>
        </Alert>
      )}

      <Box>
        <Typography variant="subtitle1" sx={{ mb: 1 }}>
          Formato
        </Typography>
        <TreeNodeList nodes={textNodes} />
      </Box>

      {cups.length > 0 && (
        <Box>
          <Typography variant="subtitle1" sx={{ mb: 2 }}>
            Diagrama
          </Typography>
          <Stack
            direction="row"
            spacing={3}
            sx={{ alignItems: 'flex-start', overflowX: 'auto', pb: 1 }}
          >
            {hasGroupStage && (
              <>
                <GroupPhaseBoxes subGroupCount={subGroupCount} />
                <ArrowForwardIcon sx={{ color: 'text.secondary', mt: 2 }} />
              </>
            )}
            <Stack spacing={3}>
              {cups.map(cup => (
                <CupBracketPreview key={cup.id} cup={cup} />
              ))}
            </Stack>
          </Stack>
        </Box>
      )}
    </Stack>
  );
}
