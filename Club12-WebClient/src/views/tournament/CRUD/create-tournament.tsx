import React, { useState } from 'react';
import {
  Card,
  CardContent,
  Typography,
  TextField,
  Button,
} from '@mui/material';
import { useTheme } from '@mui/material/styles';
import {
  AddTournamentRequest,
  ITournamentContextProps,
} from '@/modules/tournament/type/tournament';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { useError } from '@/modules/error/hooks/error.hock';
import { IErrorContextProp } from '@/modules/error/type/error';

export const CreateTournament: React.FC = () => {
  const theme = useTheme();
  const contextTournament: ITournamentContextProps = useTournament();
  const errorContext: IErrorContextProp = useError();
  const { errors, setMessage } = errorContext;

  const [tournament, setTournament] = useState<AddTournamentRequest>({
    name: '',
    description: '',
  });

  const handleCreate = () => {
    if (!tournament.name.trim()) {
      setMessage(400, ['El nombre es obligatorio']);
      return;
    }

    if (!tournament.description.trim()) {
      setMessage(400, ['La descripcion es obligatoria']);
      return;
    }

    contextTournament.addTournament(tournament);
  };

  return (
    <Card>
      <CardContent>
        <Typography
          variant="h4"
          gutterBottom
          align="center"
          color={theme.palette.primary.main}
        >
          Crear Torneo
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
          label="Nombre"
          name="name"
          variant="outlined"
          margin="normal"
          value={tournament.name}
          onChange={e => setTournament({ ...tournament, name: e.target.value })}
        />

        <TextField
          fullWidth
          label="Descripción"
          name="description"
          variant="outlined"
          margin="normal"
          multiline
          rows={4}
          value={tournament.description}
          onChange={e =>
            setTournament({ ...tournament, description: e.target.value })
          }
        />

        <Button
          fullWidth
          variant="contained"
          color="primary"
          onClick={handleCreate}
        >
          Crear
        </Button>
      </CardContent>
    </Card>
  );
};
