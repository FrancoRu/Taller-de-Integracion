import { GUID } from '@/modules/core/types/types.d';
import { useError } from '@/modules/error/hooks/error.hock';
import { useMatch } from '@/modules/match/hook/match.hook';
import { IAddMatchRequest, TypeMatch } from '@/modules/match/type/match.d';
import { useStage } from '@/modules/stage/hook/stage.hook';
import { useTeam } from '@/modules/team/hook/team.hook';
import { ITeamContextProps } from '@/modules/team/type/team.d';
import { useVenue } from '@/modules/venue/hook/venue.hook';
import { GUID_EMPTY } from '@/views/core/constants/const';
import { CustomBox } from '@/views/core/MUI/customsThemes/CustomBox';
import { RoutesNavigationViews } from '@/views/core/routes-const';
import {
  Card,
  CardContent,
  Typography,
  TextField,
  MenuItem,
  Button,
} from '@mui/material';
import dayjs from 'dayjs';
import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import utc from 'dayjs/plugin/utc';
import timezone from 'dayjs/plugin/timezone';
export const CreateMatch: React.FC = () => {
  const { venues, getAllVenues } = useVenue();
  const { teams, getTeamsByFiltered }: ITeamContextProps = useTeam();
  const { addMatch } = useMatch();

  const { stage } = useStage();
  const navigate = useNavigate();
  const { errors, setMessage } = useError();

  useEffect(() => {
    (async () => await getTeamsByFiltered({}))();
  }, [getTeamsByFiltered]);

  useEffect(() => {
    if (!stage) {
      navigate(`/${RoutesNavigationViews.Home}`, { replace: true });
      setMessage(400, [
        'Hubo un problema al cargar la información de creación de partido. Por favor, inténtelo más tarde.',
      ]);
    }
  }, [stage, navigate, setMessage]);

  useEffect(() => {
    if (!venues) (async () => await getAllVenues())();
  }, [venues, getAllVenues]);

  const [form, setForm] = useState<IAddMatchRequest>({
    matchDate: '',
    type: TypeMatch.Regular,
    homeTeamid: GUID_EMPTY,
    visitorTeamid: GUID_EMPTY,
    stageId: stage?.id as GUID,
    venueid: GUID_EMPTY,
  });

  const handleCreate = async () => {
    if (!form.matchDate) {
      setMessage(400, ['La fecha del partido es obligatoria.']);
      return;
    }
    if (form.homeTeamid === form.visitorTeamid) {
      setMessage(400, ['Los equipos no pueden ser el mismo.']);
      return;
    }

    const matchDate = new Date(form.matchDate);
    if (
      matchDate < new Date(stage!.startDate) ||
      matchDate > new Date(stage!.endDate)
    ) {
      setMessage(400, [
        'La fecha del partido debe estar dentro del rango de la etapa.',
      ]);
      return;
    }

    const res = await addMatch(form);
    if (res) navigate(`/${RoutesNavigationViews.Match}`);
  };

  const availableVisitorTeams =
    teams?.filter(t => t.id !== form.homeTeamid) ?? [];
  const availableHomeTeams =
    teams?.filter(t => t.id !== form.visitorTeamid) ?? [];

  dayjs.extend(utc);
  dayjs.extend(timezone);

  const minDateLocal = stage?.startDate
    ? dayjs.utc(stage.startDate).local().format('YYYY-MM-DDTHH:mm')
    : undefined;

  const maxDateLocal = stage?.endDate
    ? dayjs.utc(stage.endDate).local().format('YYYY-MM-DDTHH:mm')
    : undefined;
  return (
    <CustomBox>
      <Card>
        <CardContent>
          <Typography variant="h4" gutterBottom align="center">
            Crear Partido
          </Typography>

          {errors && errors.length > 0 && (
            <>
              {errors.map((e, i) => (
                <Typography
                  key={i}
                  color="error"
                  variant="body2"
                  align="center"
                  gutterBottom
                >
                  {e}
                </Typography>
              ))}
            </>
          )}

          <TextField
            fullWidth
            label="Fecha y Hora"
            name="matchDate"
            type="datetime-local"
            InputLabelProps={{ shrink: true }}
            variant="outlined"
            margin="normal"
            value={form.matchDate}
            onChange={e => setForm({ ...form, matchDate: e.target.value })}
            inputProps={{
              min: minDateLocal,
              max: maxDateLocal,
            }}
          />

          <TextField
            fullWidth
            select
            label="Cancha"
            variant="outlined"
            margin="normal"
            value={form.venueid}
            onChange={e =>
              setForm({ ...form, venueid: e.target.value as GUID })
            }
          >
            {venues?.map(v => (
              <MenuItem key={v.id} value={v.id}>
                {v.name}
              </MenuItem>
            ))}
          </TextField>

          {/* Home Team */}
          <TextField
            fullWidth
            select
            label="Equipo Local"
            variant="outlined"
            margin="normal"
            value={form.homeTeamid}
            onChange={e =>
              setForm({ ...form, homeTeamid: e.target.value as GUID })
            }
          >
            {availableHomeTeams.map(team => (
              <MenuItem key={team.id} value={team.id}>
                {team.name}
              </MenuItem>
            ))}
          </TextField>

          {/* Visitor Team */}
          <TextField
            fullWidth
            select
            label="Equipo Visitante"
            variant="outlined"
            margin="normal"
            value={form.visitorTeamid}
            onChange={e =>
              setForm({ ...form, visitorTeamid: e.target.value as GUID })
            }
          >
            {availableVisitorTeams.map(team => (
              <MenuItem key={team.id} value={team.id}>
                {team.name}
              </MenuItem>
            ))}
          </TextField>

          <Button
            fullWidth
            variant="contained"
            color="primary"
            onClick={handleCreate}
            sx={{ mt: 2 }}
          >
            Crear Partido
          </Button>
        </CardContent>
      </Card>
    </CustomBox>
  );
};
