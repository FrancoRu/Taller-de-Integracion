import { GUID } from '@/modules/core/types/types';
import { useError } from '@/modules/error/hooks/error.hock';
import { IErrorContextProp } from '@/modules/error/type/error';
import { useTeam } from '@/modules/team/hook/team.hook';
import {
  ITeamContextProps,
  IPutTeamRequest,
  ITeamResponse,
} from '@/modules/team/type/team';
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

export const EditTeam: React.FC = () => {
  const theme = useTheme();
  const { teamId: id } = useParams<{ teamId: GUID }>();
  const navigate = useNavigate();
  const { errors, setMessage }: IErrorContextProp = useError();
  const { team, putTeamById, getTeamById }: ITeamContextProps = useTeam();

  if (!id) {
    setMessage(400, ['Hubo un problema al precargar la cancha.']);
    navigate(RoutesNavigationViews.Home, { replace: true });
    return null;
  }

  const [form, setForm] = useState<IPutTeamRequest>({
    name: team?.name ?? '',
    threeLetterCode: team?.threeLetterCode ?? '',
    shirtColor: team?.shirtColor ?? '',
  });

  useEffect(() => {
    (async () => {
      const res: ITeamResponse | void = await getTeamById(id);
      if (res) {
        setForm({
          name: res.name,
          threeLetterCode: res.threeLetterCode,
          shirtColor: res.shirtColor,
        });
        return;
      }
    })();
  }, [id]);

  const handleUpdate = async () => {
    const messages: string[] = [];
    !form.name?.trim() && messages.push('El nombre es obligatorio');
    !form.threeLetterCode?.trim() && messages.push('El codigo es obligatorio');
    !form.shirtColor?.trim() &&
      messages.push('El color de la camiseta es obligatorio');

    if (messages.length > 0) {
      setMessage(400, messages);
      return;
    }

    const res: ITeamResponse | void = await putTeamById(id, form);
    if (res) {
      navigate(`/${RoutesNavigationViews.Team}`);
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
            Editar Equipo
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
            value={form.name ?? ''}
            onChange={e => setForm({ ...form, name: e.target.value })}
          />

          <TextField
            fullWidth
            label="Código (3 letras)"
            name="threeLetterCode"
            variant="outlined"
            margin="normal"
            value={form.threeLetterCode ?? ''}
            onChange={e =>
              setForm({
                ...form,
                threeLetterCode: e.target.value.toUpperCase(),
              })
            }
          />

          <TextField
            fullWidth
            label="Color de Camiseta"
            name="shirtColor"
            variant="outlined"
            margin="normal"
            value={form.shirtColor ?? ''}
            onChange={e => setForm({ ...form, shirtColor: e.target.value })}
          />

          <Button
            fullWidth
            variant="contained"
            color="primary"
            onClick={handleUpdate}
          >
            Editar
          </Button>
        </CardContent>
      </Card>
    </CustomBox>
  );
};
