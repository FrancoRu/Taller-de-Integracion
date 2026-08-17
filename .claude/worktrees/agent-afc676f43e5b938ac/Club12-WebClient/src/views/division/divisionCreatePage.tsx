import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import {
  Card,
  CardContent,
  FormControlLabel,
  Grid,
  MenuItem,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material';
import { notifySuccess, notifyWarning } from '@/modules/core/utils/confirmDialog';
import { GUID } from '@/modules/core/types/types';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { useDivision } from '@/modules/division/hook/division.hook';
import { AddDivisionRequest } from '@/modules/division/type/division';
import FormButtons from '@/views/core/components/FormButtons';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { FILTER_OPTIONS_PAGE_SIZE } from '@/modules/core/constants/pagination';

const DivisionCreatePage: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const queryTournamentId = (searchParams.get('tournamentId') ?? '') as GUID | '';

  const { tournaments, getAllTournamentsByFilter } = useTournament();
  const { addDivision } = useDivision();

  const [submitting, setSubmitting] = useState(false);
  const [name, setName] = useState('');
  const [tournamentId, setTournamentId] = useState<GUID | ''>(queryTournamentId);
  const [isCrossDivisionCup, setIsCrossDivisionCup] = useState(false);

  const isTournamentContext = Boolean(queryTournamentId);

  useEffect(() => {
    if (isTournamentContext) {
      return;
    }

    void getAllTournamentsByFilter({ pageSize: FILTER_OPTIONS_PAGE_SIZE });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isTournamentContext]);

  const tournamentOptions = useMemo(() => tournaments ?? [], [tournaments]);

  const handleCancel = useCallback(() => {
    if (queryTournamentId) {
      navigate(APP_ROUTES.panelTournamentDetail.build(queryTournamentId));
      return;
    }

    navigate(APP_ROUTES.panelDivisions);
  }, [navigate, queryTournamentId]);

  const handleCreate = useCallback(async () => {
    if (!name.trim()) {
      await notifyWarning({
        title: 'Campos incompletos',
        text: 'El nombre de la división es obligatorio.',
      });
      return;
    }

    if (!tournamentId) {
      await notifyWarning({
        title: 'Torneo requerido',
        text: 'Debes seleccionar un torneo.',
      });
      return;
    }

    setSubmitting(true);

    const payload: AddDivisionRequest = {
      name: name.trim(),
      tournamentId,
      isCrossDivisionCup,
    };

    const response = await addDivision(payload);
    setSubmitting(false);

    if (!response) {
      return;
    }

    await notifySuccess({
      title: 'División creada',
      text: 'La división se creó correctamente.',
    });

    navigate(APP_ROUTES.panelDivision.build(response.id));
  }, [name, tournamentId, isCrossDivisionCup, addDivision, navigate]);

  return (
    <Card>
      <CardContent>
        <Stack spacing={2}>
          <Typography variant="h6">Nueva división</Typography>

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

            <Grid size={12}>
              <TextField
                label="Nombre"
                value={name}
                onChange={e => setName(e.target.value)}
                required
                fullWidth
              />
            </Grid>

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
          </Grid>

          <Stack direction="row" sx={{
            justifyContent: "flex-end"
          }}>
            <FormButtons
              onCancel={handleCancel}
              onConfirm={() => void handleCreate()}
              confirmLabel="Crear"
              disabled={submitting}
            />
          </Stack>
        </Stack>
      </CardContent>
    </Card>
  );
};

export default DivisionCreatePage;
