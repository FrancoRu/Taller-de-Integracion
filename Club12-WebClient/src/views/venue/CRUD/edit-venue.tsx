import { GUID } from '@/modules/core/types/types';
import { useError } from '@/modules/error/hooks/error.hock';
import { IErrorContextProp } from '@/modules/error/type/error';
import { useVenue } from '@/modules/venue/hook/venue.hook';
import {
  IVenueContextProps,
  IVenueResponse,
  IPutVenueRequest,
} from '@/modules/venue/type/venue';
import { CustomBox } from '@/views/core/MUI/customsThemes/CustomBox';
import { RoutesNavigationViews } from '@/views/core/routes-const';
import {
  Card,
  CardContent,
  Typography,
  TextField,
  Button,
  useTheme,
} from '@mui/material';
import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';

export const EditVenue: React.FC = () => {
  const theme = useTheme();
  const { id } = useParams<{ id: GUID }>();
  const navigate = useNavigate();
  const { errors, setMessage }: IErrorContextProp = useError();
  const { putVenueById, getVenueById }: IVenueContextProps = useVenue();

  if (!id) {
    setMessage(400, ['Hubo un problema al precargar la cancha.']);
    navigate(RoutesNavigationViews.Home, { replace: true });
    return null;
  }

  const [form, setForm] = useState<IPutVenueRequest>({
    name: '',
    address: '',
    photoUrl: '',
  });
  useEffect(() => {
    (async () => {
      const res: IVenueResponse | void = await getVenueById(id);
      if (res) {
        setForm({
          name: res.name,
          address: res.address,
          photoUrl: res.photoUrl,
        });
        return;
      }
    })();
  }, [id]);

  const handleUpdate = async () => {
    if (!form.name?.trim()) {
      setMessage(400, ['El nombre es obligatorio']);
      return;
    }

    if (!form.address?.trim()) {
      setMessage(400, ['La direccion de la cancha es obligatoria']);
      return;
    }

    const res: IVenueResponse | void = await putVenueById(id, form);
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
            Editar Cancha
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
            label="Direccion"
            name="address"
            variant="outlined"
            margin="normal"
            multiline
            rows={3}
            value={form.address ?? ''}
            onChange={e => setForm({ ...form, address: e.target.value })}
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
