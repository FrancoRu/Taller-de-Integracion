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
  IAddTournamentRequest,
  ITournamentContextProps,
  ITournamentResponse,
} from '@/modules/tournament/type/tournament';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { useError } from '@/modules/error/hooks/error.hock';
import { IErrorContextProp } from '@/modules/error/type/error';
import { useNavigate } from 'react-router-dom';
import { RoutesNavigationViews } from '@/views/core/routes-const';
import { MAX_TEAM, MIN_TEAM } from '@/views/core/constants/const';

export const CreateTournament: React.FC = () => {
  const theme = useTheme();
  const contextTournament: ITournamentContextProps = useTournament();
  const errorContext: IErrorContextProp = useError();
  const { errors, setMessage } = errorContext;
  const navigate = useNavigate();

  const [tournament, setTournament] = useState<IAddTournamentRequest>({
    name: '',
    description: '',
    teamRegistrationDeadline: new Date(),
    startDate: new Date(),
    maxTeams: 0,
    minTeams: 0,
  });

  const handleCreate = async () => {
    const messages: string[] = [];

    !tournament.name.trim() && messages.push('El nombre es obligatorio');

    !tournament.description.trim() &&
      messages.push('La descripción es obligatoria');

    tournament.teamRegistrationDeadline >= tournament.startDate &&
      messages.push(
        'El plazo de inscripción debe ser anterior a la fecha de inicio'
      );

    tournament.minTeams < MIN_TEAM &&
      messages.push(`El minimo de equipos no puede ser inferior a ${MIN_TEAM}`);

    tournament.maxTeams > MAX_TEAM &&
      messages.push(`El maximo de equipos no puede ser mayor a ${MAX_TEAM}`);

    tournament.minTeams >= tournament.maxTeams &&
      messages.push('El mínimo de equipos no puede ser mayor que el máximo');

    if (messages.length > 0) {
      setMessage(400, messages);
      return;
    }

    const res: ITournamentResponse | void =
      await contextTournament.addTournament(tournament);

    if (res) {
      navigate(`/${RoutesNavigationViews.Tournament}/${res.id}`);
    }
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
          variant="outlined"
          margin="normal"
          value={tournament.name}
          onChange={e => setTournament({ ...tournament, name: e.target.value })}
        />

        <TextField
          fullWidth
          label="Descripción"
          variant="outlined"
          margin="normal"
          multiline
          rows={4}
          value={tournament.description}
          onChange={e =>
            setTournament({ ...tournament, description: e.target.value })
          }
        />

        <TextField
          fullWidth
          type="date"
          label="Plazo de inscripción"
          InputLabelProps={{ shrink: true }}
          margin="normal"
          value={
            tournament.teamRegistrationDeadline.toISOString().split('T')[0]
          }
          onChange={e =>
            setTournament({
              ...tournament,
              teamRegistrationDeadline: new Date(e.target.value),
            })
          }
        />

        <TextField
          fullWidth
          type="date"
          label="Fecha de inicio"
          InputLabelProps={{ shrink: true }}
          margin="normal"
          value={tournament.startDate.toISOString().split('T')[0]}
          onChange={e =>
            setTournament({
              ...tournament,
              startDate: new Date(e.target.value),
            })
          }
        />

        <TextField
          fullWidth
          type="number"
          label="Mínimo de equipos"
          margin="normal"
          value={tournament.minTeams}
          inputProps={{
            min: MIN_TEAM,
            max: MAX_TEAM,
          }}
          onChange={e =>
            setTournament({ ...tournament, minTeams: Number(e.target.value) })
          }
        />

        <TextField
          fullWidth
          type="number"
          label="Máximo de equipos"
          margin="normal"
          value={tournament.maxTeams}
          inputProps={{
            min: MIN_TEAM,
            max: MAX_TEAM,
          }}
          onChange={e =>
            setTournament({ ...tournament, maxTeams: Number(e.target.value) })
          }
        />

        <Button
          fullWidth
          variant="contained"
          color="primary"
          onClick={handleCreate}
          sx={{ mt: 2 }}
        >
          Crear
        </Button>
      </CardContent>
    </Card>
  );
};
