import { useError } from '@/modules/error/hooks/error.hock';
import { IErrorContextProp } from '@/modules/error/type/error';
import { usePlayer } from '@/modules/player/hook/player.hook';
import {
  IPlayerResponse,
  IPutPlayerRequest,
} from '@/modules/player/type/player';
import { CustomBox } from '@/views/core/MUI/customsThemes/CustomBox';
import { RoutesNavigationViews } from '@/views/core/routes-const';
import {
  Typography,
  Card,
  CardContent,
  TextField,
  Button,
  useTheme,
} from '@mui/material';
import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';

export const EditPlayer: React.FC = () => {
  const theme = useTheme();
  const navigate = useNavigate();
  const { errors, setMessage }: IErrorContextProp = useError();

  const { player, putPlayerById } = usePlayer();
  const [form, setForm] = useState<IPutPlayerRequest>({
    firstName: player?.firstName ?? '',
    secondName: player?.secondName ?? '',
    lastName: player?.lastName ?? '',
    documentNumber: player?.documentNumber ?? '',
    birthDate: player?.birthDate,
    phoneNumber: player?.phoneNumber,
    socialSecurity: player?.socialSecurity,
  });

  if (!player) {
    return (
      <CustomBox>
        <Typography color="text.secondary" align="center" mt={5}>
          Cargando datos del jugador...
        </Typography>
      </CustomBox>
    );
  }

  const handleUpdate = async () => {
    const messages: string[] = [];

    !form.firstName?.trim() &&
      messages.push('El primer nombre no puede estar vacío');
    !form.lastName?.trim() && messages.push('El apellido no puede estar vacío');
    !form.documentNumber?.trim() &&
      messages.push('El número de documento no puede estar vacío');

    if (messages.length > 0) {
      setMessage(400, messages);
      return;
    }

    const res: IPlayerResponse | void = await putPlayerById(player.id, form);
    if (res) {
      navigate(`/${RoutesNavigationViews.Team}/${player.teamId}`);
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
            Editar Jugador: {player.firstName} {player.lastName}
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
            variant="outlined"
            margin="normal"
            value={form.firstName}
            onChange={e => setForm({ ...form, firstName: e.target.value })}
          />

          <TextField
            fullWidth
            label="Segundo Nombre"
            variant="outlined"
            margin="normal"
            value={form.secondName ?? ''}
            onChange={e => setForm({ ...form, secondName: e.target.value })}
          />

          <TextField
            fullWidth
            label="Apellido"
            variant="outlined"
            margin="normal"
            value={form.lastName}
            onChange={e => setForm({ ...form, lastName: e.target.value })}
          />

          <TextField
            fullWidth
            label="Número de documento"
            variant="outlined"
            margin="normal"
            value={form.documentNumber}
            onChange={e => setForm({ ...form, documentNumber: e.target.value })}
          />

          <TextField
            fullWidth
            label="Fecha de nacimiento"
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
            label="Número de teléfono"
            variant="outlined"
            margin="normal"
            value={form.phoneNumber ?? ''}
            onChange={e => setForm({ ...form, phoneNumber: e.target.value })}
          />

          <TextField
            fullWidth
            label="Obra social"
            variant="outlined"
            margin="normal"
            value={form.socialSecurity ?? ''}
            onChange={e => setForm({ ...form, socialSecurity: e.target.value })}
          />

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
