import React, { useEffect, useMemo, useState } from 'react';
import Cookies from 'js-cookie';
import { decodeToken } from 'react-jwt';
import { useNavigate } from 'react-router-dom';
import {
  Button,
  Card,
  CardContent,
  CircularProgress,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useUser } from '@/modules/user/hook/user.hook';
import { GUID } from '@/modules/core/types/types';
import { UpdateUserRequest } from '@/modules/user/type/user';
import { useError } from '@/modules/error/hooks/error.hock';
import { COOKIE_SIGNIN_TOKEN } from '@/modules/core/constants/constants';

const GUID_REGEX =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

const getUserIdFromToken = (): GUID | null => {
  const accessToken = Cookies.get(COOKIE_SIGNIN_TOKEN);
  if (!accessToken) return null;

  const payload = decodeToken<Record<string, unknown>>(accessToken);
  if (!payload) return null;

  const candidateKeys = [
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier',
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/primarysid',
    'sub',
    'userId',
    'userid',
    'id',
  ];

  for (const key of candidateKeys) {
    const value = payload[key];
    if (typeof value === 'string' && GUID_REGEX.test(value)) {
      return value as GUID;
    }
  }

  return null;
};

const EditProfilePage: React.FC = () => {
  const navigate = useNavigate();
  const { user, getById, updateUser } = useUser();
  const { errors, setMessage } = useError();
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);

  const [form, setForm] = useState<UpdateUserRequest>({
    username: '',
    email: '',
    phone: '',
  });

  const targetUserId = useMemo(() => getUserIdFromToken(), []);

  useEffect(() => {
    if (!targetUserId) {
      setMessage(400, ['No se pudo identificar el usuario logueado.']);
      navigate('/panel', { replace: true });
      return;
    }

    (async () => {
      setLoading(true);
      const data = await getById(targetUserId);
      if (data) {
        setForm({
          username: data.username ?? '',
          email: data.email ?? '',
          phone: data.phoneNumber ?? '',
        });
      }
      setLoading(false);
    })();
  }, [targetUserId, getById, navigate, setMessage]);

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    setForm(prev => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async () => {
    if (!targetUserId) return;

    const payload: UpdateUserRequest = {
      username: form.username?.trim() || undefined,
      email: form.email?.trim() || undefined,
      phone: form.phone?.trim() || undefined,
    };

    if (!payload.username && !payload.email && !payload.phone) {
      setMessage(400, ['Debes completar al menos un campo para actualizar.']);
      return;
    }

    setSubmitting(true);
    const result = await updateUser(targetUserId, payload);
    setSubmitting(false);

    if (result) {
      setMessage(200, ['Perfil actualizado correctamente.']);
    }
  };

  if (loading) {
    return (
      <Card sx={{ maxWidth: 520, mx: 'auto', mt: 3 }}>
        <CardContent>
          <Stack alignItems="center" py={3}>
            <CircularProgress />
          </Stack>
        </CardContent>
      </Card>
    );
  }

  if (!user) {
    return (
      <Card sx={{ maxWidth: 520, mx: 'auto', mt: 3 }}>
        <CardContent>
          <Typography color="text.secondary">
            No se encontró el usuario.
          </Typography>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card sx={{ maxWidth: 520, mx: 'auto', mt: 3 }}>
      <CardContent>
        <Typography variant="h6" mb={2}>
          Editar perfil
        </Typography>

        {errors && errors.length > 0 && (
          <Stack spacing={0.5} mb={2}>
            {errors.map((e, i) => (
              <Typography key={i} color="error" variant="body2">
                {e}
              </Typography>
            ))}
          </Stack>
        )}

        <Stack spacing={2}>
          <TextField
            fullWidth
            label="Nombre de usuario"
            name="username"
            value={form.username ?? ''}
            onChange={handleChange}
            inputProps={{ minLength: 3, maxLength: 50 }}
          />

          <TextField
            fullWidth
            label="Email"
            name="email"
            type="email"
            value={form.email ?? ''}
            onChange={handleChange}
          />

          <TextField
            fullWidth
            label="Teléfono"
            name="phone"
            value={form.phone ?? ''}
            onChange={handleChange}
          />

          <Stack direction="row" spacing={2} justifyContent="flex-end">
            <Button
              variant="outlined"
              onClick={() => navigate('/panel')}
              disabled={submitting}
            >
              Cancelar
            </Button>
            <Button
              variant="contained"
              onClick={handleSubmit}
              disabled={submitting}
            >
              {submitting ? 'Guardando...' : 'Guardar cambios'}
            </Button>
          </Stack>
        </Stack>
      </CardContent>
    </Card>
  );
};

export default EditProfilePage;
