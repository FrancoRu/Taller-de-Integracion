import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import {
  FormControlLabel,
  Grid,
  MenuItem,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material';
import PageShell from '@/views/core/components/PageShell';
import { notifySuccess, notifyWarning } from '@/modules/core/utils/confirmDialog';
import { GUID } from '@/modules/core/types/types';
import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { ITournamentResponse } from '@/modules/tournament/type/tournament.d';
import ZoneEditor from '@/views/tournament/wizard/steps/ZoneEditor';
import { ZoneConfig, createEmptyZone } from '@/views/tournament/wizard/types';
import { buildZoneDivision } from '@/views/tournament/wizard/submitWizard';
import FormButtons from '@/views/core/components/FormButtons';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { FILTER_OPTIONS_PAGE_SIZE } from '@/modules/core/constants/pagination';

const DivisionCreatePage: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const queryTournamentId = (searchParams.get('tournamentId') ?? '') as GUID | '';

  const { tournaments, getAllTournamentsByFilter, getTournamentById, addFullDivision } =
    useTournament();

  const [submitting, setSubmitting] = useState(false);
  const [tournamentId, setTournamentId] = useState<GUID | ''>(queryTournamentId);
  const [resolvedTournament, setResolvedTournament] = useState<ITournamentResponse | null>(null);
  const [isCrossDivisionCup, setIsCrossDivisionCup] = useState(false);
  const [zone, setZone] = useState<ZoneConfig>(createEmptyZone());

  const isTournamentContext = Boolean(queryTournamentId);

  useEffect(() => {
    if (isTournamentContext) {
      return;
    }

    void getAllTournamentsByFilter({ pageSize: FILTER_OPTIONS_PAGE_SIZE });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isTournamentContext]);

  // Resolve the selected tournament's full record (status + category) so the
  // new division's category always matches (HU-48) and the page can warn
  // before submitting to a tournament whose structure is already frozen.
  useEffect(() => {
    if (!tournamentId) {
      setResolvedTournament(null);
      return;
    }

    void getTournamentById(tournamentId).then(fetched => {
      setResolvedTournament(fetched ?? null);
    });
  }, [tournamentId, getTournamentById]);

  // Only tournaments still accepting structural changes can receive a new
  // division (enforced server-side too, EnsureTournamentAllowsDivisionAsync)
  // — filtering them out of the picker means there is never a selectable
  // option that will just fail on submit.
  const tournamentOptions = useMemo(
    () =>
      (tournaments ?? []).filter(
        tournament => tournament.status === TournamentStatus.OpenForRegistration
      ),
    [tournaments]
  );

  const isStructureFrozen =
    Boolean(resolvedTournament) &&
    resolvedTournament?.status !== TournamentStatus.OpenForRegistration;

  // Real browser-history back — takes the admin back to exactly the page
  // (and tab/sub-nav state) they came from, whatever that was.
  const handleCancel = useCallback(() => {
    navigate(-1);
  }, [navigate]);

  const handleCreate = useCallback(async () => {
    if (!zone.name.trim()) {
      await notifyWarning({
        title: 'Campos incompletos',
        text: 'El nombre de la división es obligatorio.',
      });
      return;
    }

    if (!tournamentId || !resolvedTournament) {
      await notifyWarning({
        title: 'Torneo requerido',
        text: 'Debes seleccionar un torneo.',
      });
      return;
    }

    if (isStructureFrozen) {
      await notifyWarning({
        title: 'Estructura congelada',
        text: 'Este torneo ya no acepta nuevas divisiones (solo se pueden crear mientras la inscripción está abierta).',
      });
      return;
    }

    setSubmitting(true);

    const payload = buildZoneDivision(
      zone,
      new Date(resolvedTournament.startDate),
      resolvedTournament.category
    );
    if (isCrossDivisionCup) {
      payload.isCrossDivisionCup = true;
    }

    const response = await addFullDivision(tournamentId, payload);
    setSubmitting(false);

    if (!response) {
      return;
    }

    await notifySuccess({
      title: 'División creada',
      text: 'La división se creó correctamente.',
    });

    navigate(APP_ROUTES.panelDivision.build(response.slug));
  }, [
    zone,
    tournamentId,
    resolvedTournament,
    isStructureFrozen,
    isCrossDivisionCup,
    addFullDivision,
    navigate,
  ]);

  return (
    <PageShell title="Nueva división" maxWidth="md">
      <Stack spacing={2}>
        <Grid container spacing={2}>
          {!isTournamentContext && (
            <Grid size={12}>
              <TextField
                select
                required
                label="Torneo"
                value={tournamentId}
                onChange={e => setTournamentId(e.target.value as GUID)}
                fullWidth
              >
                <MenuItem value="" disabled>
                  Seleccionar torneo
                </MenuItem>
                {tournamentOptions.map(tournament => (
                  <MenuItem key={tournament.id} value={tournament.id}>
                    {tournament.name}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
          )}

          {isStructureFrozen && (
            <Grid size={12}>
              <Typography variant="body2" sx={{ color: 'warning.main' }}>
                Este torneo ya no está en inscripción abierta — no se pueden agregar
                divisiones nuevas.
              </Typography>
            </Grid>
          )}

          <Grid size={12}>
            <FormControlLabel
              control={
                <Switch
                  checked={isCrossDivisionCup}
                  onChange={e => setIsCrossDivisionCup(e.target.checked)}
                />
              }
              label="Copa cruzada (agrupa equipos de todas las divisiones del torneo)"
            />
          </Grid>

          <Grid size={12}>
            <ZoneEditor zone={zone} onChange={updates => setZone(prev => ({ ...prev, ...updates }))} />
          </Grid>
        </Grid>

        <Stack direction="row" sx={{ justifyContent: 'flex-end' }}>
          <FormButtons
            onCancel={handleCancel}
            onConfirm={() => void handleCreate()}
            confirmLabel="Crear"
            disabled={submitting || isStructureFrozen}
          />
        </Stack>
      </Stack>
    </PageShell>
  );
};

export default DivisionCreatePage;
