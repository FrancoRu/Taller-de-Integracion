import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Grid, MenuItem, Stack, TextField } from '@mui/material';
import PageShell from '@/views/core/components/PageShell';
import { notifySuccess, notifyWarning } from '@/modules/core/utils/confirmDialog';
import { GUID } from '@/modules/core/types/types';
import { useStage } from '@/modules/stage/hook/stage.hook';
import { useTeam } from '@/modules/team/hook/team.hook';
import { useVenue } from '@/modules/venue/hook/venue.hook';
import { useMatch } from '@/modules/match/hook/match.hook';
import { IAddMatchRequest } from '@/modules/match/type/match.d';
import FormButtons from '@/views/core/components/FormButtons';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { FILTER_OPTIONS_PAGE_SIZE } from '@/modules/core/constants/pagination';

const MatchCreatePage: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const queryStageId = (searchParams.get('stageId') ?? '') as GUID | '';

  const { stages, getStagesByFilters } = useStage();
  const { teams, getTeamsByFiltered } = useTeam();
  const { venues, getAllVenues } = useVenue();
  const { addMatch } = useMatch();

  const [submitting, setSubmitting] = useState(false);
  const [stageId, setStageId] = useState<GUID | ''>(queryStageId);
  const [homeTeamId, setHomeTeamId] = useState<GUID | ''>('');
  const [visitorTeamId, setVisitorTeamId] = useState<GUID | ''>('');
  const [matchDate, setMatchDate] = useState('');
  const [venueId, setVenueId] = useState<GUID | ''>('');

  const isStageContext = Boolean(queryStageId);

  useEffect(() => {
    if (isStageContext) {
      return;
    }

    void getStagesByFilters({ pageSize: FILTER_OPTIONS_PAGE_SIZE });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isStageContext]);

  useEffect(() => {
    void getAllVenues();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (!stageId) {
      return;
    }

    void getTeamsByFiltered({ stageId, pageSize: FILTER_OPTIONS_PAGE_SIZE });
  }, [stageId, getTeamsByFiltered]);

  const stageOptions = useMemo(() => stages ?? [], [stages]);
  const teamOptions = useMemo(() => teams ?? [], [teams]);
  const venueOptions = useMemo(() => venues ?? [], [venues]);

  const handleCancel = useCallback(() => {
    if (queryStageId) {
      navigate(APP_ROUTES.panelStage.build(queryStageId));
      return;
    }

    navigate(APP_ROUTES.panelMatches);
  }, [navigate, queryStageId]);

  const handleCreate = useCallback(async () => {
    if (!stageId) {
      await notifyWarning({ title: 'Fase requerida', text: 'Debes seleccionar una fase.' });
      return;
    }

    if (!homeTeamId || !visitorTeamId) {
      await notifyWarning({
        title: 'Equipos requeridos',
        text: 'Debes seleccionar el equipo local y el visitante.',
      });
      return;
    }

    if (homeTeamId === visitorTeamId) {
      await notifyWarning({
        title: 'Equipos inválidos',
        text: 'El equipo local y el visitante no pueden ser el mismo.',
      });
      return;
    }

    if (!matchDate) {
      await notifyWarning({ title: 'Fecha requerida', text: 'Debes completar la fecha del partido.' });
      return;
    }

    setSubmitting(true);

    const payload: IAddMatchRequest = {
      matchDate: new Date(matchDate).toISOString(),
      homeTeamId,
      visitorTeamId,
      stageId,
      venueId: venueId || undefined,
    };

    const response = await addMatch(payload);
    setSubmitting(false);

    if (!response) {
      return;
    }

    await notifySuccess({ title: 'Partido creado', text: 'El partido se creó correctamente.' });

    handleCancel();
  }, [stageId, homeTeamId, visitorTeamId, matchDate, venueId, addMatch, handleCancel]);

  return (
    <PageShell title="Nuevo partido" maxWidth="md">
      <Stack spacing={2}>
        <Grid container spacing={2}>
            {!isStageContext && (
              <Grid size={12}>
                <TextField
                  select
                  required
                  label="Fase"
                  value={stageId}
                  onChange={e => {
                    setStageId(e.target.value as GUID);
                    setHomeTeamId('');
                    setVisitorTeamId('');
                  }}
                  fullWidth
                >
                  <MenuItem value="" disabled>
                    Seleccionar fase
                  </MenuItem>
                  {stageOptions.map(stage => (
                    <MenuItem key={stage.id} value={stage.id}>
                      {stage.name}
                    </MenuItem>
                  ))}
                </TextField>
              </Grid>
            )}

            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
              <TextField
                select
                required
                label="Equipo local"
                value={homeTeamId}
                onChange={e => setHomeTeamId(e.target.value as GUID)}
                disabled={!stageId}
                fullWidth
              >
                <MenuItem value="" disabled>
                  Seleccionar equipo
                </MenuItem>
                {teamOptions.map(team => (
                  <MenuItem key={team.id} value={team.id}>
                    {team.name}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>

            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
              <TextField
                select
                required
                label="Equipo visitante"
                value={visitorTeamId}
                onChange={e => setVisitorTeamId(e.target.value as GUID)}
                disabled={!stageId}
                fullWidth
              >
                <MenuItem value="" disabled>
                  Seleccionar equipo
                </MenuItem>
                {teamOptions.map(team => (
                  <MenuItem key={team.id} value={team.id}>
                    {team.name}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>

            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
              <TextField
                label="Fecha y hora"
                type="datetime-local"
                value={matchDate}
                onChange={e => setMatchDate(e.target.value)}
                required
                fullWidth
                slotProps={{
                  inputLabel: { shrink: true }
                }}
              />
            </Grid>

            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
              <TextField
                select
                label="Cancha (opcional)"
                value={venueId}
                onChange={e => setVenueId(e.target.value as GUID)}
                fullWidth
              >
                <MenuItem value="">Sin especificar</MenuItem>
                {venueOptions.map(venue => (
                  <MenuItem key={venue.id} value={venue.id}>
                    {venue.name}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
        </Grid>

        <Stack direction="row" sx={{ justifyContent: 'flex-end' }}>
          <FormButtons
            onCancel={handleCancel}
            onConfirm={() => void handleCreate()}
            confirmLabel="Crear"
            disabled={submitting}
          />
        </Stack>
      </Stack>
    </PageShell>
  );
};

export default MatchCreatePage;
