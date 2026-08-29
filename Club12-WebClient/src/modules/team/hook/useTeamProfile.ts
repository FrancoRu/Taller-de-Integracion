import { useEffect, useState } from 'react';
import { GUID } from '@/modules/core/types/types';
import { teamService } from '@/modules/team/service/team.service';
import { scorerService } from '@/modules/scorer/service/scorer.service';
import { championService } from '@/modules/champion/service/champion.service';
import {
  TeamMatch,
  TeamParticipation,
  TeamSummary,
} from '@/modules/team/type/teamProfile.d';
import { IScorerByPlayerResponse } from '@/modules/scorer/type/scorer.d';
import { IChampionHistory } from '@/modules/champion/type/champion.d';

/** How many top scorers the "Goleadores" block requests/shows. */
const TOP_SCORERS_LIMIT = 5;

/**
 * The tournaments a team has taken part in (newest first), driving the profile's
 * season/tournament selector. Fetches once per team.
 */
export const useTeamParticipations = (
  idOrSlug?: string
): { participations: TeamParticipation[]; loading: boolean } => {
  const [participations, setParticipations] = useState<TeamParticipation[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!idOrSlug) return;
    let cancelled = false;

    const run = async () => {
      setLoading(true);
      try {
        const res = await teamService.getTeamParticipations(idOrSlug);
        if (!cancelled) setParticipations(res.data ?? []);
      } catch {
        if (!cancelled) setParticipations([]);
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    void run();
    return () => {
      cancelled = true;
    };
  }, [idOrSlug]);

  return { participations, loading };
};

/**
 * A team's standing plus fixture for the selected tournament. Both refetch when
 * the active tournament changes; `summary` is `null` when the team has no
 * standing yet. Skips fetching until a tournament is selected.
 */
export const useTeamStandings = (
  idOrSlug: string | undefined,
  tournamentId: GUID | undefined
): { summary: TeamSummary | null; matches: TeamMatch[]; loading: boolean } => {
  const [summary, setSummary] = useState<TeamSummary | null>(null);
  const [matches, setMatches] = useState<TeamMatch[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!idOrSlug || !tournamentId) {
      setSummary(null);
      setMatches([]);
      return;
    }
    let cancelled = false;

    const run = async () => {
      setLoading(true);
      try {
        const [summaryRes, matchesRes] = await Promise.all([
          teamService.getTeamSummary(idOrSlug, tournamentId),
          teamService.getTeamMatches(idOrSlug, tournamentId),
        ]);
        if (!cancelled) {
          setSummary(summaryRes.data ?? null);
          setMatches(matchesRes.data ?? []);
        }
      } catch {
        if (!cancelled) {
          setSummary(null);
          setMatches([]);
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    void run();
    return () => {
      cancelled = true;
    };
  }, [idOrSlug, tournamentId]);

  return { summary, matches, loading };
};

/**
 * The team's top scorers for the selected tournament, reusing the shared scorer
 * ranking endpoint. Skips fetching until both the team and tournament are known.
 */
export const useTeamScorers = (
  teamId: GUID | undefined,
  tournamentId: GUID | undefined
): { scorers: IScorerByPlayerResponse[]; loading: boolean } => {
  const [scorers, setScorers] = useState<IScorerByPlayerResponse[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!teamId || !tournamentId) {
      setScorers([]);
      return;
    }
    let cancelled = false;

    const run = async () => {
      setLoading(true);
      try {
        const res = await scorerService.getScorersByPlayerFiltered({
          teamId,
          tournamentId,
          pageNumber: 1,
          pageSize: TOP_SCORERS_LIMIT,
        });
        if (!cancelled) setScorers(res.data?.items ?? []);
      } catch {
        if (!cancelled) setScorers([]);
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    void run();
    return () => {
      cancelled = true;
    };
  }, [teamId, tournamentId]);

  return { scorers, loading };
};

/**
 * The titles this team has won, derived from the public champions history by
 * filtering to entries whose champion is this team. Fetches once per team.
 */
export const useTeamTitles = (
  teamId: GUID | undefined
): { titles: IChampionHistory[]; loading: boolean } => {
  const [titles, setTitles] = useState<IChampionHistory[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!teamId) return;
    let cancelled = false;

    const run = async () => {
      setLoading(true);
      try {
        const res = await championService.getChampionsHistory();
        const mine = (res.data ?? []).filter(
          entry => entry.championTeam.teamId === teamId
        );
        if (!cancelled) setTitles(mine);
      } catch {
        if (!cancelled) setTitles([]);
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    void run();
    return () => {
      cancelled = true;
    };
  }, [teamId]);

  return { titles, loading };
};
