import React, { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Button,
  Chip,
  IconButton,
  MenuItem,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import { useClub } from '@/modules/club/hook/club.hook';
import { GUID } from '@/modules/core/types/types';
import PageShell from '@/views/core/components/PageShell';
import { TableSkeleton } from '@/views/core/components/skeletons';
import TeamLogo from '@/views/core/components/TeamLogo';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { confirmAction, notifyWarning } from '@/modules/core/utils/confirmDialog';
import { EditIcon } from '@/views/core/MUI/icons/icons';

interface ClubSeasonRow {
  key: string;
  teamId: GUID;
  teamName: string;
  threeLetterCode: string;
  teamSlug: string;
  tournamentName: string;
  /** ISO start date of the season; '' for a team with no registered season. */
  startDate: string;
}

/**
 * Club history / trajectory view (HU-99). Shows the club header and, per
 * season, the team and the tournaments it was registered in.
 */
const ClubHistoryPage: React.FC = () => {
  const { idOrSlug } = useParams<{ idOrSlug: string }>();
  const navigate = useNavigate();
  const {
    club,
    getClubHistory,
    allClubs,
    getAllClubs,
    linkClubParent,
    unlinkClubParent,
    renameClub,
  } = useClub();
  const [loading, setLoading] = useState(false);
  const [linking, setLinking] = useState(false);
  const [selectedParentId, setSelectedParentId] = useState<GUID | ''>('');
  const [editingName, setEditingName] = useState(false);
  const [nameDraft, setNameDraft] = useState('');
  const [renaming, setRenaming] = useState(false);

  useEffect(() => {
    if (!idOrSlug) {
      return;
    }

    const fetchHistory = async () => {
      setLoading(true);
      await getClubHistory(idOrSlug);
      setLoading(false);
    };

    void fetchHistory();
  }, [getClubHistory, idOrSlug]);

  // A club with no parent and no squads of its own can link to a parent
  // institution (HU-125b) — the flat-tree rule means an institution's own
  // page never shows this picker, only a childless, parentless club's does.
  const canLinkParent =
    !!club && !club.parentClub && club.childClubs.length === 0;

  useEffect(() => {
    if (canLinkParent) {
      void getAllClubs();
    }
  }, [canLinkParent, getAllClubs]);

  const parentCandidates = useMemo(
    () => allClubs.filter(candidate => candidate.id !== club?.id),
    [allClubs, club?.id]
  );

  const handleLinkParent = async () => {
    if (!club || !selectedParentId) {
      return;
    }
    setLinking(true);
    await linkClubParent(club.id, selectedParentId);
    setLinking(false);
    setSelectedParentId('');
  };

  const handleUnlinkParent = async () => {
    if (!club) {
      return;
    }
    const confirmed = await confirmAction({
      title: 'Desvincular club matriz',
      text: `¿Quitar el vínculo entre "${club.name}" y "${club.parentClub?.name ?? ''}"?`,
      confirmButtonText: 'Sí, desvincular',
    });
    if (!confirmed) {
      return;
    }
    setLinking(true);
    await unlinkClubParent(club.id);
    setLinking(false);
  };

  const startEditingName = () => {
    if (!club) {
      return;
    }
    setNameDraft(club.name);
    setEditingName(true);
  };

  const cancelEditingName = () => {
    setEditingName(false);
    setNameDraft('');
  };

  const handleRenameClub = async () => {
    if (!club) {
      return;
    }
    const trimmed = nameDraft.trim();
    if (!trimmed) {
      void notifyWarning({
        title: 'Nombre requerido',
        text: 'El nombre del club es obligatorio.',
      });
      return;
    }
    setRenaming(true);
    const result = await renameClub(club.id, trimmed);
    setRenaming(false);
    if (result) {
      setEditingName(false);
    }
  };

  // Canonicalise the URL to the club slug. The team page's "Ver historial del
  // club" button navigates here with the club GUID (all it carries), so once
  // the real slug is known, replace the history entry with the slug URL.
  useEffect(() => {
    if (club && idOrSlug && club.slug && idOrSlug !== club.slug) {
      navigate(APP_ROUTES.panelClub.build(club.slug), { replace: true });
    }
  }, [club, idOrSlug, navigate]);

  // One row per (team, season) pair, so the table reads as the club's
  // season-by-season trajectory. A team with no registered season still
  // shows a single row with a placeholder. Rows across every team are then
  // sorted newest-season-first; placeholder rows (no start date) sort last.
  const rows = useMemo<ClubSeasonRow[]>(() => {
    if (!club) {
      return [];
    }

    const unsorted = club.teams.flatMap(team => {
      if (team.seasons.length === 0) {
        return [
          {
            key: `${team.teamId}-none`,
            teamId: team.teamId,
            teamName: team.name,
            threeLetterCode: team.threeLetterCode,
            teamSlug: team.slug,
            tournamentName: '—',
            startDate: '',
          },
        ];
      }

      return team.seasons.map(season => ({
        key: `${team.teamId}-${season.tournamentId}`,
        teamId: team.teamId,
        teamName: team.name,
        threeLetterCode: team.threeLetterCode,
        teamSlug: team.slug,
        tournamentName: season.tournamentName ?? '—',
        startDate: season.startDate ?? '',
      }));
    });

    return unsorted.sort((a, b) => b.startDate.localeCompare(a.startDate));
  }, [club]);

  if (loading) {
    return (
      <PageShell title="Historial del club">
        <TableSkeleton columns={3} />
      </PageShell>
    );
  }

  if (!club) {
    return (
      <PageShell
        title="Club no encontrado"
        back={{ label: 'Volver', onClick: () => navigate(-1) }}
      >
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          No fue posible cargar el historial del club.
        </Typography>
      </PageShell>
    );
  }

  return (
    <PageShell
      back={{ label: 'Volver', onClick: () => navigate(-1) }}
    >
        <Stack
          direction="row"
          spacing={1.5}
          sx={{ alignItems: 'center', mb: 3 }}
        >
          <TeamLogo
            teamName={club.name}
            logoUrl={club.logoUrl ?? ''}
            size={44}
          />
          <div style={{ flexGrow: 1 }}>
            {editingName ? (
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                <TextField
                  size="small"
                  autoFocus
                  value={nameDraft}
                  onChange={event => setNameDraft(event.target.value)}
                  disabled={renaming}
                />
                <Button
                  size="small"
                  variant="contained"
                  disabled={renaming}
                  onClick={() => void handleRenameClub()}
                >
                  Guardar
                </Button>
                <Button size="small" disabled={renaming} onClick={cancelEditingName}>
                  Cancelar
                </Button>
              </Stack>
            ) : (
              <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
                <Typography variant="h4" component="h1">
                  {club.name}
                </Typography>
                <IconButton
                  size="small"
                  aria-label="Editar nombre del club"
                  onClick={startEditingName}
                >
                  <EditIcon fontSize="small" />
                </IconButton>
              </Stack>
            )}
            <Typography variant="body2" sx={{ color: 'text.secondary' }}>
              Historial del club
            </Typography>
          </div>
        </Stack>

        {club.parentClub && (
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 3 }}>
            <Chip
              label={`Escuadra de ${club.parentClub.name}`}
              onClick={() =>
                navigate(APP_ROUTES.panelClub.build(club.parentClub!.slug))
              }
              clickable
            />
            <Button size="small" disabled={linking} onClick={() => void handleUnlinkParent()}>
              Desvincular
            </Button>
          </Stack>
        )}

        {club.childClubs.length > 0 && (
          <Stack spacing={1} sx={{ mb: 3 }}>
            <Typography variant="subtitle2">Escuadras</Typography>
            <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
              {club.childClubs.map(childClub => (
                <Chip
                  key={childClub.id}
                  label={childClub.name}
                  onClick={() =>
                    navigate(APP_ROUTES.panelClub.build(childClub.slug))
                  }
                  clickable
                />
              ))}
            </Stack>
          </Stack>
        )}

        {canLinkParent && (
          <Stack
            direction="row"
            spacing={1.5}
            sx={{ alignItems: 'center', mb: 3 }}
          >
            <TextField
              select
              size="small"
              label="Vincular con club matriz"
              value={selectedParentId}
              onChange={event =>
                setSelectedParentId(event.target.value as GUID)
              }
              disabled={linking}
              sx={{ minWidth: 260 }}
              helperText={
                parentCandidates.length === 0
                  ? 'No hay otros clubes disponibles.'
                  : undefined
              }
            >
              {parentCandidates.map(candidate => (
                <MenuItem key={candidate.id} value={candidate.id}>
                  {candidate.name}
                </MenuItem>
              ))}
            </TextField>
            <Button
              variant="outlined"
              disabled={linking || !selectedParentId}
              onClick={() => void handleLinkParent()}
            >
              Vincular
            </Button>
          </Stack>
        )}

        {rows.length > 0 ? (
          <TableContainer>
            <Table size="small" aria-label="Historial por temporada">
              <TableHead>
                <TableRow>
                  <TableCell>Equipo</TableCell>
                  <TableCell>Código</TableCell>
                  <TableCell>Temporada</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {rows.map(row => (
                  <TableRow key={row.key} hover>
                    <TableCell>
                      <Typography
                        component="button"
                        onClick={() =>
                          navigate(
                            APP_ROUTES.panelTeamDetail.build(row.teamSlug)
                          )
                        }
                        sx={{
                          border: 0,
                          background: 'none',
                          color: 'primary.main',
                          cursor: 'pointer',
                          p: 0,
                          font: 'inherit',
                          textAlign: 'left',
                        }}
                      >
                        {row.teamName}
                      </Typography>
                    </TableCell>
                    <TableCell>{row.threeLetterCode}</TableCell>
                    <TableCell>{row.tournamentName}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        ) : (
          <Typography variant="body2" sx={{ color: 'text.secondary' }}>
            Este club todavía no tiene temporadas registradas.
          </Typography>
        )}
    </PageShell>
  );
};

export default ClubHistoryPage;
