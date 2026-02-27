import React, { useEffect } from 'react';
import { ITeamContextProps } from '@/modules/team/type/team';
import { useTeam } from '@/modules/team/hook/team.hook';
import { useVenue } from '@/modules/venue/hook/venue.hook';
import { GUID } from '@/modules/core/types/types';
import { Typography, TextField, MenuItem } from '@mui/material';

import dayjs from 'dayjs';
import utc from 'dayjs/plugin/utc';
import timezone from 'dayjs/plugin/timezone';

export const MatchForm: React.FC<{
  stageId: GUID;
  errors: string[] | null;
  startDate: string;
  endDate: string;
  showTeams: boolean | undefined;
  form: any;
  setForm: any;
}> = ({ stageId, errors, startDate, endDate, form, setForm, showTeams }) => {
  const { teams, getTeamsByFiltered }: ITeamContextProps = useTeam();

  const { venues, getAllVenues } = useVenue();

  useEffect(() => {
    if (!venues) (async () => await getAllVenues())();
  }, [venues, getAllVenues]);

  useEffect(() => {
    if (stageId && showTeams) {
      (async () => await getTeamsByFiltered({ stageId: stageId }))();
    }
  }, [getTeamsByFiltered, stageId]);

  const availableVisitorTeams =
    teams?.filter(t => t.id !== form.homeTeamid) ?? [];
  const availableHomeTeams =
    teams?.filter(t => t.id !== form.visitorTeamid) ?? [];

  dayjs.extend(utc);
  dayjs.extend(timezone);

  const minDateLocal = startDate
    ? dayjs.utc(startDate).local().format('YYYY-MM-DDTHH:mm')
    : undefined;

  const maxDateLocal = endDate
    ? dayjs.utc(endDate).local().format('YYYY-MM-DDTHH:mm')
    : undefined;

  return (
    <>
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
        onChange={e => setForm({ ...form, venueid: e.target.value as GUID })}
      >
        {venues?.map(v => (
          <MenuItem key={v.id} value={v.id}>
            {v.name}
          </MenuItem>
        ))}
      </TextField>

      {showTeams && (
        <>
          <TextField
            fullWidth
            select
            label="Equipo Local"
            variant="outlined"
            margin="normal"
            value={form.homeTeamid}
            hidden={!showTeams}
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

          <TextField
            fullWidth
            select
            label="Equipo Visitante"
            variant="outlined"
            margin="normal"
            value={form.visitorTeamid}
            hidden={!showTeams}
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
        </>
      )}
    </>
  );
};
