import { GUID } from '@/modules/core/types/types';
import { useMatch } from '@/modules/match/hook/match.hook';
import {
  IEditMatch,
  IMatchContextProps,
  IMatchResponse,
  IPutMatchRequest,
} from '@/modules/match/type/match';
import { TextField, MenuItem, Button } from '@mui/material';
import dayjs from 'dayjs';
import utc from 'dayjs/plugin/utc';
import timezone from 'dayjs/plugin/timezone';
import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { RoutesNavigationViews } from '@/views/core/routes-const';
import { useVenue } from '@/modules/venue/hook/venue.hook';
import { IVenueContextProps } from '@/modules/venue/type/venue';

export const EditNotStartedMatch: React.FC<IEditMatch> = ({
  id,
  matchDate,
  venue,
  startDate,
  endDate,
}) => {
  dayjs.extend(utc);
  dayjs.extend(timezone);
  const { putMatchByMatchId }: IMatchContextProps = useMatch();

  const { venues, getAllVenues }: IVenueContextProps = useVenue();
  const [form, setForm] = useState<IPutMatchRequest>({
    matchDate: dayjs.utc(matchDate).format('YYYY-MM-DDTHH:mm'),
    venueId: venue?.id,
  });

  useEffect(() => {
    if (!venues) {
      (async () => {
        await getAllVenues();
      })();
    }
  }, [getAllVenues]);

  const navigate = useNavigate();

  const handleUpdate = async () => {
    const messages: string[] = [];
    !form.matchDate && messages.push('La fecha del partido es obligatoria.');

    form.matchDate &&
      (new Date(form.matchDate) < new Date(startDate) ||
        new Date(form.matchDate) > new Date(endDate)) &&
      messages.push(
        'La fecha del partido debe estar dentro del rango de la etapa.'
      );

    const res: IMatchResponse | void = await putMatchByMatchId(id, form);

    if (res) navigate(`/${RoutesNavigationViews.Match}/${id}`);
  };
  const minDateLocal = startDate
    ? dayjs.utc(startDate).format('YYYY-MM-DDTHH:mm')
    : undefined;

  const maxDateLocal = endDate
    ? dayjs.utc(endDate).format('YYYY-MM-DDTHH:mm')
    : undefined;

  return (
    <>
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
        value={form.venueId}
        onChange={e => setForm({ ...form, venueId: e.target.value as GUID })}
      >
        {venues?.map(v => (
          <MenuItem key={v.id} value={v.id}>
            {v.name}
          </MenuItem>
        ))}
      </TextField>
      <Button
        fullWidth
        variant="contained"
        color="primary"
        onClick={handleUpdate}
        sx={{ mt: 2 }}
      >
        Editar Partido
      </Button>
    </>
  );
};
