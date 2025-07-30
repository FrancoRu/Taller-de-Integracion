import { useError } from '@/modules/error/hooks/error.hock';
import { IErrorContextProp } from '@/modules/error/type/error';
import { useTeam } from '@/modules/team/hook/team.hook';
import {
  IAddTeamRequest,
  ITeamContextProps,
  ITeamResponse,
} from '@/modules/team/type/team';
import { CustomBox } from '@/views/core/MUI/customsThemes/CustomBox';
import { RoutesNavigationViews } from '@/views/core/routes-const';
import {
  Button,
  Card,
  CardContent,
  Grid,
  TextField,
  Typography,
  useTheme,
} from '@mui/material';
import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';

export const CreateTeam: React.FC = () => {
  const theme = useTheme();
  const { errors, setMessage }: IErrorContextProp = useError();
  const navigate = useNavigate();

  const { addTeam }: ITeamContextProps = useTeam();

  const [form, setForm] = useState<IAddTeamRequest>({
    name: '',
    threeLetterCode: '',
    shirtColor: '',
    logo: new File([], ''),
  });

  const handleCreate = async () => {
    const messages: string[] = [];
    !form.name.trim() && messages.push('El nombre es obligatorio.');
    !form.threeLetterCode.trim() && messages.push('El código es obligatorio.');
    !form.shirtColor.trim() &&
      messages.push('El color de camiseta es obligatorio.');
    (!form.logo || form.logo.size === 0 || !form.logo.name) &&
      messages.push('El logo es obligatorio.');

    if (messages.length > 0) {
      setMessage(400, messages);
      return;
    }

    const res: ITeamResponse | void = await addTeam(form);

    if (res) {
      navigate(`/${RoutesNavigationViews.Team}/${res.id}`);
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
            Crear Equipo
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

          <Grid container spacing={2}>
            <Grid item xs={12} md={6}>
              <TextField
                fullWidth
                label="Nombre del equipo"
                name="name"
                variant="outlined"
                margin="normal"
                value={form.name}
                onChange={e => setForm({ ...form, name: e.target.value })}
              />

              <TextField
                fullWidth
                label="Código de 3 letras"
                name="threeLetterCode"
                variant="outlined"
                margin="normal"
                value={form.threeLetterCode}
                onChange={e =>
                  setForm({ ...form, threeLetterCode: e.target.value })
                }
              />

              <TextField
                fullWidth
                label="Color de camiseta"
                name="shirtColor"
                variant="outlined"
                margin="normal"
                value={form.shirtColor}
                onChange={e => setForm({ ...form, shirtColor: e.target.value })}
              />
            </Grid>

            <Grid
              item
              xs={12}
              md={6}
              display="flex"
              flexDirection="column"
              justifyContent="center"
              alignItems="center"
            >
              <Button
                variant="outlined"
                component="label"
                fullWidth
                sx={{
                  mt: 2,
                  mb: 2,
                  height: '100%',
                  width: '75%',
                  borderStyle: 'dashed',
                  borderWidth: 2,
                  borderColor: 'primary.main',
                  fontSize: '2rem',
                  fontWeight: 'bold',
                  color: 'primary.main',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                }}
              >
                +
                <input
                  type="file"
                  hidden
                  accept="image/*"
                  onChange={e =>
                    setForm({
                      ...form,
                      logo: e.target.files
                        ? e.target.files[0]
                        : new File([], ''),
                    })
                  }
                />
              </Button>

              {form.logo && form.logo.name && (
                <Typography variant="body2" align="center" sx={{ mt: 1 }}>
                  Archivo seleccionado: {form.logo.name}
                </Typography>
              )}
            </Grid>
          </Grid>

          <Button
            fullWidth
            variant="contained"
            color="primary"
            onClick={handleCreate}
            sx={{ mt: 3 }}
          >
            Crear
          </Button>
        </CardContent>
      </Card>
    </CustomBox>
  );
};
