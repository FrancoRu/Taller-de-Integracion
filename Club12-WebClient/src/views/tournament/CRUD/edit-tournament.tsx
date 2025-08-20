import { GUID } from '@/modules/core/types/types';
import { formatDateTimeInput } from '@/modules/core/utils/formatDate';
import { useError } from '@/modules/error/hooks/error.hock';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { IPutTournamentRequest } from '@/modules/tournament/type/tournament.d';
import theme from '@/theme';
import { MAX_TEAM, MIN_TEAM } from '@/views/core/constants/const';
import { CustomBox } from '@/views/core/MUI/customsThemes/CustomBox';
import { RoutesNavigationViews } from '@/views/core/routes-const';
import {
  Card,
  CardContent,
  Typography,
  TextField,
  Button,
  Grid,
} from '@mui/material';
import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';

export const EditTournament: React.FC = () => {
  const { errors, setMessage } = useError();
  const { tournament, getTournamentById, putTournamentById } = useTournament();
  const { tournamentId: id } = useParams<{ tournamentId: GUID }>();
  const navigate = useNavigate();

  const [editTournament, setEditTournament] = useState<IPutTournamentRequest>({
    name: tournament?.name ?? '',
    description: tournament?.description ?? '',
    teamRegistrationDeadline:
      tournament?.teamRegistrationDeadline ?? new Date(),
    startDate: tournament?.startDate ?? new Date(),
    maxTeams: tournament?.maxTeams ?? 0,
    minTeams: tournament?.minTeams ?? 0,
  });

  const tournamentStarted = tournament
    ? new Date() >= new Date(tournament.startDate)
    : false;

  useEffect(() => {
    if (!id) {
      navigate(RoutesNavigationViews.Home, { replace: true });
      return;
    }

    if (!tournament) {
      (async () => {
        const res = await getTournamentById(id);
        if (!res) {
          navigate(RoutesNavigationViews.Home, { replace: true });
        }
      })();
    }
  }, [id, tournament, getTournamentById, navigate]);

  useEffect(() => {
    if (tournament) {
      setEditTournament({
        name: tournament.name ?? '',
        description: tournament.description ?? '',
        teamRegistrationDeadline: tournament.teamRegistrationDeadline,
        startDate: tournament.startDate,
        maxTeams: tournament.maxTeams,
        minTeams: tournament.minTeams,
      });
    }
  }, [tournament]);

  const handleUpdate = async () => {
    const messages: string[] = [];

    !editTournament.name.trim() && messages.push('El nombre es obligatorio');

    !editTournament.description.trim() &&
      messages.push('La descripción es obligatoria');
    if (!tournamentStarted) {
      !editTournament.description.trim() &&
        messages.push('La descripción es obligatoria');

      if (editTournament.teamRegistrationDeadline >= editTournament.startDate) {
        messages.push(
          'El plazo de inscripción debe ser anterior a la fecha de inicio'
        );
      }

      editTournament.minTeams < MIN_TEAM &&
        messages.push(
          `El minimo de equipos no puede ser inferior a ${MIN_TEAM}`
        );

      editTournament.maxTeams > MAX_TEAM &&
        messages.push(`El maximo de equipos no puede ser mayor a ${MAX_TEAM}`);

      if (editTournament.minTeams > editTournament.maxTeams) {
        messages.push('El mínimo de equipos no puede ser mayor que el máximo');
      }
    }

    if (!tournament || !tournament.id) {
      messages.push('No se encontró el torneo para actualizar.');
    }

    if (messages.length > 0) {
      setMessage(400, messages);
      return;
    }

    await putTournamentById(tournament!.id, editTournament);
    navigate(`/${RoutesNavigationViews.Tournament}/${tournament!.id}`);
  };

  return (
    <CustomBox>
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

          {errors?.map((e, i) => (
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

          {!tournamentStarted && (
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
              disabled={tournamentStarted}
            />
          )}

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

          {!tournamentStarted && (
            <>
              <TextField
                fullWidth
                name="startDate"
                label="Fecha de Inicio"
                type="datetime-local"
                value={formatDateTimeInput(editTournament.startDate)}
                onChange={e =>
                  setEditTournament({
                    ...editTournament,
                    startDate: new Date(e.target.value),
                  })
                }
                disabled={tournamentStarted}
                margin="normal"
              />

              <TextField
                fullWidth
                name="teamRegistrationDeadline"
                label="Fecha limite de Inscripción"
                type="datetime-local"
                value={formatDateTimeInput(
                  editTournament.teamRegistrationDeadline
                )}
                onChange={e =>
                  setEditTournament({
                    ...editTournament,
                    teamRegistrationDeadline: new Date(e.target.value),
                  })
                }
                disabled={tournamentStarted}
                margin="normal"
              />

              <Grid container spacing={2}>
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    name="minTeams"
                    label="Mínimo de Equipos"
                    type="number"
                    value={editTournament.minTeams}
                    onChange={e =>
                      setEditTournament({
                        ...editTournament,
                        minTeams: Number(e.target.value),
                      })
                    }
                    disabled={tournamentStarted}
                    margin="normal"
                  />
                </Grid>

                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    name="maxTeams"
                    label="Máximo de Equipos"
                    type="number"
                    value={editTournament.maxTeams}
                    onChange={e =>
                      setEditTournament({
                        ...editTournament,
                        maxTeams: Number(e.target.value),
                      })
                    }
                    disabled={tournamentStarted}
                    margin="normal"
                  />
                </Grid>
              </Grid>
            </>
          )}

          <Button
            fullWidth
            variant="contained"
            color="primary"
            onClick={handleUpdate}
          >
            Guardar Cambios
          </Button>
        </CardContent>
      </Card>
    </CustomBox>
  );
};
