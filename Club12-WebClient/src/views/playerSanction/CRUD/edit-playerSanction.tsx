import { GUID } from '@/modules/core/types/types';
import { useError } from '@/modules/error/hooks/error.hock';
import { IErrorContextProp } from '@/modules/error/type/error';
import { usePlayerSanction } from '@/modules/playerSanction/hook/playerSanction.hook';
import {
  IPlayerSanctionContextProps,
  IPlayerSanctionResponse,
  IPutPlayerSanction,
} from '@/modules/playerSanction/type/playerSanction';
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
import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';

export const EditPlayerSanction: React.FC = () => {
  const { playerSanctionId: id } = useParams<{ playerSanctionId: GUID }>();
  const theme = useTheme();
  const navigate = useNavigate();
  const { errors, setMessage }: IErrorContextProp = useError();

  const {
    playerSanction,
    putPlayerSanctionById,
    getPlayerSanctionById,
  }: IPlayerSanctionContextProps = usePlayerSanction();

  const [form, setForm] = useState<IPutPlayerSanction>({
    duration: playerSanction?.duration ?? 1,
    description: playerSanction?.description ?? '',
  });

  useEffect(() => {
    if (id) {
      (async () => {
        const res: IPlayerSanctionResponse | void =
          await getPlayerSanctionById(id);
        if (res) {
          setForm({
            duration: res.duration,
            description: res.description,
          });
        }
      })();
    }
  }, [id]);

  if (!playerSanction) {
    return (
      <CustomBox>
        <Typography color="text.secondary" align="center" mt={5}>
          Cargando datos de la sancion...
        </Typography>
      </CustomBox>
    );
  }

  const handleUpdate = async () => {
    const messages: string[] = [];

    form.duration <= 0 &&
      messages.push('La duracion de la sancion no puede ser menor a 1.');
    !form.description?.trim() &&
      messages.push('La descripcion es obligatoria.');

    if (messages.length > 0) {
      setMessage(400, messages);
      return;
    }

    const res: IPlayerSanctionResponse | void = await putPlayerSanctionById(
      playerSanction.id,
      form
    );
    if (res) {
      navigate(`/${RoutesNavigationViews.Match}/${playerSanction.matchId}`);
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
            Editar Sancion
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
            label="Duración"
            type="number"
            variant="outlined"
            margin="normal"
            value={form.duration}
            inputProps={{
              min: 1,
            }}
            onChange={e =>
              setForm(prev => ({ ...prev, duration: Number(e.target.value) }))
            }
          />

          <TextField
            fullWidth
            label="Descripcion"
            variant="outlined"
            margin="normal"
            value={form.description}
            onChange={e => setForm({ ...form, description: e.target.value })}
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
