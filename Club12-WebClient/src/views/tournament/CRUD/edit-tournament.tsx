import { useError } from '@/modules/error/hooks/error.hock';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { IPutTournamentRequest } from '@/modules/tournament/type/tournament.d';
import theme from '@/theme';
import { RoutesNavigationViews } from '@/views/core/routes-const';
import {
  Card,
  CardContent,
  Typography,
  TextField,
  Button,
} from '@mui/material';
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';

export const EditTournament: React.FC = () => {
  const { errors, setMessage } = useError();
  const { tournament, putTournamentById } = useTournament();
  const navigate = useNavigate();
  const [editTournament, setEditTournament] = useState<IPutTournamentRequest>({
    name: tournament?.name ?? '',
    description: tournament?.description ?? '',
  });

  if (!tournament) {
    navigate('/', { replace: true });
    return;
  }

  useEffect(() => {
    if (tournament) {
      setEditTournament({
        name: tournament.name,
        description: tournament.description,
      });
    }
  }, [tournament]);

  const handleCreate = async () => {
    if (!editTournament.name.trim()) {
      setMessage(400, ['El nombre es obligatorio']);
      return;
    }

    if (!editTournament.description.trim()) {
      setMessage(400, ['La descripcion es obligatoria']);
      return;
    }

    await putTournamentById(tournament?.id, editTournament);
    navigate(`/${RoutesNavigationViews.Tournament}/${tournament.id}`);
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
          Editar Torneo
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
          value={editTournament.name}
          onChange={e =>
            setEditTournament({ ...editTournament, name: e.target.value })
          }
        />

        <TextField
          fullWidth
          label="Descripción"
          name="description"
          variant="outlined"
          margin="normal"
          multiline
          rows={4}
          value={editTournament.description}
          onChange={e =>
            setEditTournament({
              ...editTournament,
              description: e.target.value,
            })
          }
        />

        <Button
          fullWidth
          variant="contained"
          color="primary"
          onClick={handleCreate}
        >
          Editar
        </Button>
      </CardContent>
    </Card>
  );
};
