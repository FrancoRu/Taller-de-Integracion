import { useCallback, useEffect, useRef, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import {
  Box,
  Button,
  Card,
  CardActionArea,
  CardContent,
  Chip,
  Grid,
  IconButton,
  Stack,
  Typography,
} from '@mui/material';
import { useSeason } from '@/modules/season/hook/season.hook';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import {
  IPutSeasonRequest,
  ISeasonResponse,
  ISeasonTournament,
} from '@/modules/season/type/season';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';
import {
  TOURNAMENT_STATUS_COLOR,
  TOURNAMENT_STATUS_LABEL,
  resolveTournamentStatus,
} from '@/modules/tournament/utils/tournamentDisplay';
import CategoryChip from '@/views/core/components/CategoryChip';
import NewEntityButton from '@/views/core/components/NewEntityButton';
import PageShell from '@/views/core/components/PageShell';
import { DetailSkeleton } from '@/views/core/components/skeletons';
import { DeleteIcon } from '@/views/core/MUI/icons/icons';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import {
  confirmDelete,
  notifySuccess,
  notifyWarning,
} from '@/modules/core/utils/confirmDialog';
import SeasonFormDialog, { SeasonFormState } from '@/views/season/SeasonFormDialog';

const EMPTY_SEASON_FORM: SeasonFormState = { name: '', year: '' };

const parseYear = (value: string): number | null => {
  const trimmed = value.trim();
  if (!trimmed) {
    return null;
  }
  const parsed = Number(trimmed);
  return Number.isFinite(parsed) ? parsed : null;
};

function TournamentCard({
  tournament,
  onDelete,
}: {
  tournament: ISeasonTournament;
  onDelete: (tournament: ISeasonTournament) => void;
}) {
  return (
    <Card sx={{ height: '100%', position: 'relative' }}>
      <IconButton
        aria-label={`Eliminar ${tournament.name}`}
        size="small"
        onClick={e => {
          // The card body is itself a Link (CardActionArea) — stop the click
          // from bubbling into it and navigating away before the confirm
          // dialog even opens.
          e.preventDefault();
          e.stopPropagation();
          onDelete(tournament);
        }}
        sx={{
          position: 'absolute',
          top: 4,
          right: 4,
          zIndex: 1,
          bgcolor: 'background.paper',
          '&:hover': { bgcolor: 'error.main', color: 'error.contrastText' },
        }}
      >
        <DeleteIcon fontSize="small" />
      </IconButton>
      <CardActionArea
        component={Link}
        to={APP_ROUTES.panelTournamentDetail.build(
          tournament.slug ?? tournament.id
        )}
        sx={{ height: '100%' }}
      >
        <CardContent sx={{ width: '100%' }}>
          <Typography variant="h6" component="h3" sx={{ lineHeight: 1.3, mb: 1, pr: 4 }}>
            {tournament.name}
          </Typography>
          <Chip
            label={TOURNAMENT_STATUS_LABEL[resolveTournamentStatus(tournament.status)]}
            color={TOURNAMENT_STATUS_COLOR[resolveTournamentStatus(tournament.status)]}
            size="small"
          />
        </CardContent>
      </CardActionArea>
    </Card>
  );
}

function CategorySection({
  category,
  tournaments,
  onDeleteTournament,
}: {
  category: TournamentCategory;
  tournaments: ISeasonTournament[];
  onDeleteTournament: (tournament: ISeasonTournament) => void;
}) {
  if (tournaments.length === 0) {
    return null;
  }

  return (
    <Box component="section" sx={{ mb: 4 }}>
      <Box sx={{ mb: 2 }}>
        <CategoryChip category={category} size="medium" />
      </Box>
      <Grid container spacing={3}>
        {tournaments.map(tournament => (
          <Grid key={tournament.id} size={{ xs: 12, sm: 6, md: 4 }}>
            <TournamentCard tournament={tournament} onDelete={onDeleteTournament} />
          </Grid>
        ))}
      </Grid>
    </Box>
  );
}

export default function AdminSeasonDetailPage() {
  const { seasonId } = useParams<{ seasonId: string }>();
  const navigate = useNavigate();
  const { getSeasonById, putSeasonById } = useSeason();
  const { deleteTournamentById } = useTournament();

  const [loading, setLoading] = useState(true);
  const [season, setSeason] = useState<ISeasonResponse | null>(null);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [editSubmitting, setEditSubmitting] = useState(false);
  const [seasonForm, setSeasonForm] = useState<SeasonFormState>(EMPTY_SEASON_FORM);
  const getSeasonByIdRef = useRef(getSeasonById);

  useEffect(() => {
    getSeasonByIdRef.current = getSeasonById;
  }, [getSeasonById]);

  const fetchSeason = useCallback(async () => {
    if (!seasonId) {
      setLoading(false);
      return;
    }

    setLoading(true);
    const response = await getSeasonByIdRef.current(seasonId);
    setSeason(response ?? null);
    setLoading(false);
  }, [seasonId]);

  useEffect(() => {
    void fetchSeason();
  }, [fetchSeason]);

  const handleSeasonFieldChange = useCallback(
    (field: keyof SeasonFormState, value: string) => {
      setSeasonForm(prev => ({ ...prev, [field]: value }));
    },
    []
  );

  const openEditDialog = () => {
    if (!season) return;

    setSeasonForm({
      name: season.name,
      year: season.year != null ? String(season.year) : '',
    });
    setEditDialogOpen(true);
  };

  const handleEditSubmit = async () => {
    if (!season) return;

    if (!seasonForm.name.trim()) {
      void notifyWarning({
        title: 'Campos incompletos',
        text: 'El nombre es obligatorio.',
      });
      return;
    }

    setEditSubmitting(true);
    const payload: IPutSeasonRequest = {
      name: seasonForm.name.trim(),
      year: parseYear(seasonForm.year),
    };

    const updated = await putSeasonById(season.id, payload);
    setEditSubmitting(false);

    if (!updated) {
      return;
    }

    setEditDialogOpen(false);
    await fetchSeason();
    await notifySuccess({
      title: 'Temporada actualizada',
      text: 'La temporada se actualizó correctamente.',
    });
  };

  const handleDeleteTournament = async (tournament: ISeasonTournament) => {
    const confirmed = await confirmDelete({
      title: `¿Eliminar "${tournament.name}"?`,
      text: '¡Usted no podrá revertir este cambio!',
    });

    if (!confirmed) {
      return;
    }

    const deleted = await deleteTournamentById(tournament.id);
    if (!deleted) {
      return;
    }

    await fetchSeason();
    await notifySuccess({
      title: '¡Eliminado!',
      text: 'El torneo ha sido eliminado.',
    });
  };

  if (loading) {
    return (
      <PageShell title="Temporada">
        <DetailSkeleton />
      </PageShell>
    );
  }

  if (!season) {
    return (
      <PageShell
        title="Temporada no encontrada"
        back={{
          label: 'Volver a temporadas',
          onClick: () => navigate(APP_ROUTES.panelSeasons),
        }}
      >
        <Typography sx={{ color: 'text.secondary' }}>
          La temporada que buscás no existe o ya no está disponible.
        </Typography>
      </PageShell>
    );
  }

  // The wizard is pre-scoped with the RESOLVED season id (a GUID), not the URL
  // param — which may be a slug — so the "Temporada" select preselects the
  // right season regardless of how this page was reached.
  const startNewTournament = () => {
    navigate(APP_ROUTES.panelTournamentWizard, {
      state: { seasonId: season.id },
    });
  };

  const tournaments = season.tournaments ?? [];
  const masculineTournaments = tournaments.filter(
    t => t.category === TournamentCategory.Masculine
  );
  const feminineTournaments = tournaments.filter(
    t => t.category === TournamentCategory.Feminine
  );

  return (
    <PageShell
      back={{
        label: 'Volver a temporadas',
        onClick: () => navigate(APP_ROUTES.panelSeasons),
      }}
      actions={
        <Stack direction="row" spacing={1.5}>
          <Button variant="outlined" color="primary" onClick={openEditDialog}>
            Editar temporada
          </Button>
          <NewEntityButton type="Torneo" onClick={startNewTournament} />
        </Stack>
      }
    >
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 3 }}>
        <Typography variant="h4" component="h1" sx={{ fontWeight: 'bold' }}>
          {season.name}
        </Typography>
        {season.year != null && (
          <Chip label={season.year} color="primary" size="small" />
        )}
      </Box>

      {tournaments.length === 0 ? (
        <Typography sx={{ color: 'text.secondary' }}>
          Esta temporada todavía no tiene torneos asociados. Creá el primero con
          el botón “Nuevo torneo”.
        </Typography>
      ) : (
        <>
          <CategorySection
            category={TournamentCategory.Masculine}
            tournaments={masculineTournaments}
            onDeleteTournament={tournament => void handleDeleteTournament(tournament)}
          />
          <CategorySection
            category={TournamentCategory.Feminine}
            tournaments={feminineTournaments}
            onDeleteTournament={tournament => void handleDeleteTournament(tournament)}
          />
        </>
      )}

      <SeasonFormDialog
        open={editDialogOpen}
        title="Editar temporada"
        confirmLabel="Guardar"
        form={seasonForm}
        submitting={editSubmitting}
        onFieldChange={handleSeasonFieldChange}
        onClose={() => setEditDialogOpen(false)}
        onConfirm={() => void handleEditSubmit()}
      />
    </PageShell>
  );
}
