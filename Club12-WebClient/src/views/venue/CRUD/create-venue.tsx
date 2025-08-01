import { useError } from '@/modules/error/hooks/error.hock';
import { IErrorContextProp } from '@/modules/error/type/error';
import { useVenue } from '@/modules/venue/hook/venue.hook';
import {
  IAddVenueRequest,
  IVenueContextProps,
  IVenueResponse,
} from '@/modules/venue/type/venue';
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

export const CreateVenue: React.FC = () => {
  const theme = useTheme();
  const { errors, setMessage }: IErrorContextProp = useError();
  const navigate = useNavigate();
  const { addVenue }: IVenueContextProps = useVenue();

  const [venue, setVenue] = useState<IAddVenueRequest>({
    name: '',
    address: '',
    imageFile: new File([], ''),
  });

  const handleCreate = async () => {
    const messages: string[] = [];
    !venue.name.trim() && messages.push('El nombre es obligatorio.');
    !venue.address.trim() &&
      messages.push('La dirección de la cancha es obligatoria.');
    (!venue.imageFile || venue.imageFile.size === 0 || !venue.imageFile.name) &&
      messages.push('La imagen es obligatoria.');

    if (messages.length > 0) {
      setMessage(400, messages);
      return;
    }

    const res: IVenueResponse | void = await addVenue(venue);
    if (res) {
      navigate(`/${RoutesNavigationViews.Venue}`);
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
            Crear Cancha
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
                label="Nombre"
                name="name"
                variant="outlined"
                margin="normal"
                value={venue.name}
                onChange={e => setVenue({ ...venue, name: e.target.value })}
              />

              <TextField
                fullWidth
                label="Dirección"
                name="address"
                variant="outlined"
                margin="normal"
                multiline
                rows={3}
                value={venue.address}
                onChange={e => setVenue({ ...venue, address: e.target.value })}
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
                    setVenue({
                      ...venue,
                      imageFile: e.target.files
                        ? e.target.files[0]
                        : new File([], ''),
                    })
                  }
                />
              </Button>

              {venue.imageFile && venue.imageFile.name && (
                <Typography variant="body2" align="center" sx={{ mt: 1 }}>
                  Archivo seleccionado: {venue.imageFile.name}
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
