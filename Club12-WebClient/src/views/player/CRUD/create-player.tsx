import { useError } from '@/modules/error/hooks/error.hock';
import { IErrorContextProp } from '@/modules/error/type/error';
import { usePlayer } from '@/modules/player/hook/player.hook';
import {
  IAddPlayerRequest,
  IPlayerContextProps,
  IPlayerResponse,
} from '@/modules/player/type/player';
import { useTeam } from '@/modules/team/hook/team.hook';
import { ITeamContextProps } from '@/modules/team/type/team';
import { CustomBox } from '@/views/core/MUI/customsThemes/CustomBox';
import { RoutesNavigationViews } from '@/views/core/routes-const';
import {
  Button,
  Card,
  CardContent,
  TextField,
  Typography,
  useTheme,
} from '@mui/material';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';

export const CreatePlayer: React.FC = () => {
  const { team }: ITeamContextProps = useTeam();
  const { addPlayer }: IPlayerContextProps = usePlayer();
  const theme = useTheme();
  const { errors, setMessage }: IErrorContextProp = useError();
  const navigate = useNavigate();

  const [form, setForm] = useState<IAddPlayerRequest>({
    firstName: '',
    secondName: '',
    lastName: '',
    documentNumber: '',
    teamId: team?.id,
    birthDate: undefined,
    phoneNumber: '',
    socialSecurity: '',
  });

  if (!team) {
    navigate(RoutesNavigationViews.Home, { replace: true });
    setMessage(404, [
      'Hubo un problema al cargar el equipo al cual se le debe agregar el nuevo jugador.',
    ]);
    return null;
  }

  const handleCreate = async () => {
    const messages: string[] = [];

    if (!form.firstName.trim()) {
      messages.push('El nombre no puede estar vacío');
    }
    if (!form.lastName.trim()) {
      messages.push('El apellido no puede estar vacío');
    }
    if (!form.documentNumber.trim()) {
      messages.push('El documento no puede estar vacío');
    }

    if (messages.length > 0) {
      setMessage(400, messages);
      return;
    }

    const res: IPlayerResponse | void = await addPlayer(form);
    if (res) {
      navigate(`${RoutesNavigationViews.Team}/${team.id}`);
    }
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
            Crear Jugador
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
            label="Primer Nombre"
            name="firstName"
            variant="outlined"
            margin="normal"
            value={form.firstName}
            onChange={e => setForm({ ...form, firstName: e.target.value })}
          />

          <TextField
            fullWidth
            label="Segundo Nombre (opcional)"
            name="secondName"
            variant="outlined"
            margin="normal"
            value={form.secondName}
            onChange={e => setForm({ ...form, secondName: e.target.value })}
          />

          <TextField
            fullWidth
            label="Apellido"
            name="lastName"
            variant="outlined"
            margin="normal"
            value={form.lastName}
            onChange={e => setForm({ ...form, lastName: e.target.value })}
          />

          <TextField
            fullWidth
            label="Número de Documento"
            name="documentNumber"
            variant="outlined"
            margin="normal"
            value={form.documentNumber}
            onChange={e => setForm({ ...form, documentNumber: e.target.value })}
          />

          <TextField
            fullWidth
            label="Fecha de Nacimiento"
            name="birthDate"
            type="date"
            InputLabelProps={{ shrink: true }}
            margin="normal"
            value={
              form.birthDate
                ? new Date(form.birthDate).toISOString().split('T')[0]
                : ''
            }
            onChange={e =>
              setForm({ ...form, birthDate: new Date(e.target.value) })
            }
          />

          <TextField
            fullWidth
            label="Número de Teléfono"
            name="phoneNumber"
            variant="outlined"
            margin="normal"
            value={form.phoneNumber}
            onChange={e => setForm({ ...form, phoneNumber: e.target.value })}
          />

          <TextField
            fullWidth
            label="Seguro Social"
            name="socialSecurity"
            variant="outlined"
            margin="normal"
            value={form.socialSecurity}
            onChange={e => setForm({ ...form, socialSecurity: e.target.value })}
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
    </CustomBox>
  );
};
