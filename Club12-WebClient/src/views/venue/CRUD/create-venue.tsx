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
    photoUrl: 'https://example.com/images/venue-placeholder.jpg',
  });

  const handleCreate = async () => {
    if (!venue.name.trim()) {
      setMessage(400, ['El nombre es obligatorio']);
      return;
    }

    if (!venue.address.trim()) {
      setMessage(400, ['La direccion de la cancha es obligatoria']);
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

          <TextField
            fullWidth
            label="Nombre"
            name="name"
            variant="outlined"
            margin="normal"
            value={venue.name ?? ''}
            onChange={e => setVenue({ ...venue, name: e.target.value })}
          />

          <TextField
            fullWidth
            label="Direccion"
            name="address"
            variant="outlined"
            margin="normal"
            multiline
            rows={3}
            value={venue.address ?? ''}
            onChange={e => setVenue({ ...venue, address: e.target.value })}
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
    </CustomBox>
  );
};
