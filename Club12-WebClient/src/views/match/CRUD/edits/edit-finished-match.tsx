import { useError } from '@/modules/error/hooks/error.hock';
import { IErrorContextProp } from '@/modules/error/type/error';
import { useMatch } from '@/modules/match/hook/match.hook';
import {
  IEditMatch,
  IMatchContextProps,
  IMatchResponse,
  IPutMatchScoreRequest,
} from '@/modules/match/type/match';
import { RoutesNavigationViews } from '@/views/core/routes-const';
import { Button, TextField } from '@mui/material';
import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';

export const EditFinishedMatch: React.FC<IEditMatch> = ({
  id,
  homeScore,
  visitorScore,
}) => {
  const { putMatchScoreByMatchId }: IMatchContextProps = useMatch();
  const [form, setForm] = useState<IPutMatchScoreRequest>({
    homeScore: homeScore ?? 0,
    visitorScore: visitorScore ?? 0,
  });
  const { setMessage }: IErrorContextProp = useError();

  const [homeScoreError, setHomeScoreError] = useState<string | null>(null);
  const [visitorScoreError, setVisitorScoreError] = useState<string | null>(
    null
  );

  const navigate = useNavigate();
  const handleScoreChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>,
    field: 'homeScore' | 'visitorScore'
  ) => {
    const value = e.target.value;
    const errorSetter =
      field === 'homeScore' ? setHomeScoreError : setVisitorScoreError;

    if (value === '') {
      setForm(prevForm => ({ ...prevForm, [field]: '' }));
      errorSetter(null);
      return;
    }

    const parsedValue = parseInt(value, 10);

    if (isNaN(parsedValue)) {
      errorSetter('Solo se permiten números.');
      setForm(prevForm => ({ ...prevForm, [field]: value }));
    } else if (parsedValue < 0) {
      errorSetter('Solo se permiten números positivos.');
      setForm(prevForm => ({ ...prevForm, [field]: parsedValue }));
    } else if (parsedValue.toString() !== value) {
      errorSetter('Solo se permiten números enteros.');
      setForm(prevForm => ({ ...prevForm, [field]: value }));
    } else {
      setForm(prevForm => ({ ...prevForm, [field]: parsedValue }));
      errorSetter(null);
    }
  };

  const handleUpdate = async () => {
    const messages: string[] = [];

    const finalHomeScore =
      typeof form.homeScore === 'string'
        ? parseInt(form.homeScore, 10)
        : form.homeScore;
    const finalVisitorScore =
      typeof form.visitorScore === 'string'
        ? parseInt(form.visitorScore, 10)
        : form.visitorScore;

    if (
      finalHomeScore === undefined ||
      isNaN(finalHomeScore) ||
      finalHomeScore < 0
    ) {
      messages.push(
        'El puntaje del equipo local es requerido y debe ser un entero positivo.'
      );
    }
    if (
      finalVisitorScore === undefined ||
      isNaN(finalVisitorScore) ||
      finalVisitorScore < 0
    ) {
      messages.push(
        'El puntaje del equipo visitante es requerido y debe ser un entero positivo.'
      );
    }

    if (messages.length > 0) {
      setMessage(400, messages);
      return;
    }

    const res: IMatchResponse | void = await putMatchScoreByMatchId(id, {
      homeScore: finalHomeScore as number,
      visitorScore: finalVisitorScore as number,
    });
    if (res) {
      navigate(`/${RoutesNavigationViews.Match}/${id}`);
    }
  };

  return (
    <>
      <TextField
        fullWidth
        label="Puntos equipo local"
        name="homeScore"
        type="number"
        InputLabelProps={{ shrink: true }}
        variant="outlined"
        margin="normal"
        value={form.homeScore}
        onChange={e => handleScoreChange(e, 'homeScore')}
        inputProps={{
          min: 0,
          step: 1,
        }}
        error={!!homeScoreError}
        helperText={homeScoreError}
      />

      <TextField
        fullWidth
        label="Puntos equipo visitante"
        name="visitorScore"
        type="number"
        InputLabelProps={{ shrink: true }}
        variant="outlined"
        margin="normal"
        value={form.visitorScore}
        onChange={e => handleScoreChange(e, 'visitorScore')}
        inputProps={{
          min: 0,
          step: 1,
        }}
        error={!!visitorScoreError}
        helperText={visitorScoreError}
      />

      <Button
        fullWidth
        variant="contained"
        color="primary"
        onClick={handleUpdate}
        sx={{ mt: 2 }}
        disabled={
          !!homeScoreError ||
          !!visitorScoreError ||
          String(form.homeScore) === '' ||
          String(form.visitorScore) === ''
        }
      >
        Editar Partido
      </Button>
    </>
  );
};
