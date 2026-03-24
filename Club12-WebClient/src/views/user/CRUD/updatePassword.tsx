import React, { useMemo, useState } from 'react';
import Cookies from 'js-cookie';
import { decodeToken } from 'react-jwt';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Button,
  Card,
  CardContent,
  Divider,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useError } from '../../../modules/error/hooks/error.hock';
import { useUser } from '../../../modules/user/hook/user.hook';
import { COOKIE_SIGNIN_TOKEN } from '../../../modules/core/constants/constants';
import { GUID } from '../../../modules/core/types/types';

interface PasswordForm {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

interface UpdatePasswordProps {
  requireCurrentPassword?: boolean;
}

const GUID_REGEX =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

const getPasswordPolicyState = (password: string) => {
  const uniqueCharsCount = new Set(password).size;

  return {
    requiredLength: password.length >= 8,
    requireUppercase: /[A-Z]/.test(password),
    requireLowercase: /[a-z]/.test(password),
    requireDigit: /\d/.test(password),
    requireNonAlphanumeric: /[^a-zA-Z0-9]/.test(password),
    requiredUniqueChars: uniqueCharsCount >= 2,
  };
};

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

  const allClaimValues = Object.values(payload).flatMap(value =>
    Array.isArray(value) ? value : [value]
  );

  for (const value of allClaimValues) {
    if (typeof value === 'string' && GUID_REGEX.test(value)) {
      return value as GUID;
    }
  }

  return null;
};

const UpdatePassword: React.FC<UpdatePasswordProps> = ({
  requireCurrentPassword,
}) => {
  const { userId: routeUserId } = useParams<{ userId: string }>();
  const navigate = useNavigate();
  const { setMessage, errors } = useError();
  const { changeUserPassword } = useUser();

  const [submitting, setSubmitting] = useState(false);
  const [form, setForm] = useState<PasswordForm>({
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
  });

  const passwordPolicy = useMemo(
    () => getPasswordPolicyState(form.newPassword),
    [form.newPassword]
  );

  const loggedUserId = useMemo(() => getUserIdFromToken(), []);

  const targetUserId = useMemo(() => {
    if (routeUserId && GUID_REGEX.test(routeUserId)) {
      return routeUserId as GUID;
    }
    return loggedUserId;
  }, [routeUserId, loggedUserId]);

  const shouldRequireCurrentPassword = useMemo(() => {
    if (typeof requireCurrentPassword === 'boolean') {
      return requireCurrentPassword;
    }

    if (!loggedUserId || !targetUserId) {
      return true;
    }

    return loggedUserId === targetUserId;
  }, [requireCurrentPassword, loggedUserId, targetUserId]);

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    setForm(prev => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async () => {
    const messages: string[] = [];

    if (!targetUserId) {
      messages.push(
        'No se pudo identificar el usuario para cambiar el password.'
      );
    }

    if (shouldRequireCurrentPassword && !form.currentPassword) {
      messages.push('La contraseña actual es obligatoria.');
    }

    if (!form.newPassword) {
      messages.push('La nueva contraseña es obligatoria.');
    }

    if (!passwordPolicy.requiredLength) {
      messages.push('La contraseña debe tener al menos 8 caracteres.');
    }

    if (!passwordPolicy.requireUppercase) {
      messages.push(
        'La contraseña debe contener al menos una letra mayúscula.'
      );
    }

    if (!passwordPolicy.requireLowercase) {
      messages.push(
        'La contraseña debe contener al menos una letra minúscula.'
      );
    }

    if (!passwordPolicy.requireDigit) {
      messages.push('La contraseña debe contener al menos un número.');
    }

    if (!passwordPolicy.requireNonAlphanumeric) {
      messages.push(
        'La contraseña debe contener al menos un carácter no alfanumérico.'
      );
    }

    if (!passwordPolicy.requiredUniqueChars) {
      messages.push(
        'La contraseña debe contener al menos 2 caracteres únicos.'
      );
    }

    if (!form.confirmPassword) {
      messages.push('La confirmación de contraseña es obligatoria.');
    }

    if (form.newPassword && form.confirmPassword) {
      if (form.newPassword !== form.confirmPassword) {
        messages.push('La confirmación no coincide con la nueva contraseña.');
      }
    }

    if (messages.length > 0) {
      setMessage(400, messages);
      return;
    }

    setSubmitting(true);
    const ok = await changeUserPassword(targetUserId as GUID, {
      newPassword: form.newPassword,
      currentPassword: shouldRequireCurrentPassword
        ? form.currentPassword
        : undefined,
    });
    setSubmitting(false);

    if (ok) {
      setMessage(200, ['Contraseña actualizada correctamente.']);
      setForm({ currentPassword: '', newPassword: '', confirmPassword: '' });
      navigate('/panel/configuracion/cambiar-password');
    }
  };

  return (
    <Card sx={{ maxWidth: 520, mx: 'auto', mt: 2 }}>
      <CardContent>
        <Typography variant="h6" mb={2}>
          Cambiar password
        </Typography>

        {errors && errors.length > 0 && (
          <Stack spacing={0.5} mb={2}>
            {errors.map((error, index) => (
              <Typography key={index} color="error" variant="body2">
                {error}
              </Typography>
            ))}
          </Stack>
        )}

        <Stack spacing={2}>
          {shouldRequireCurrentPassword && (
            <TextField
              fullWidth
              label="Contraseña actual"
              name="currentPassword"
              type="password"
              value={form.currentPassword}
              onChange={handleChange}
            />
          )}

          <TextField
            fullWidth
            label="Nueva contraseña"
            name="newPassword"
            type="password"
            value={form.newPassword}
            onChange={handleChange}
          />

          <Stack spacing={0.5}>
            <Typography variant="subtitle2">Reglas de contraseña</Typography>
            <Divider />
            <Typography
              variant="body2"
              color={
                passwordPolicy.requiredLength
                  ? 'success.main'
                  : 'text.secondary'
              }
            >
              {passwordPolicy.requiredLength ? '✓' : '•'} Mínimo 8 caracteres
            </Typography>
            <Typography
              variant="body2"
              color={
                passwordPolicy.requireUppercase
                  ? 'success.main'
                  : 'text.secondary'
              }
            >
              {passwordPolicy.requireUppercase ? '✓' : '•'} Al menos una
              mayúscula
            </Typography>
            <Typography
              variant="body2"
              color={
                passwordPolicy.requireLowercase
                  ? 'success.main'
                  : 'text.secondary'
              }
            >
              {passwordPolicy.requireLowercase ? '✓' : '•'} Al menos una
              minúscula
            </Typography>
            <Typography
              variant="body2"
              color={
                passwordPolicy.requireDigit ? 'success.main' : 'text.secondary'
              }
            >
              {passwordPolicy.requireDigit ? '✓' : '•'} Al menos un número
            </Typography>
            <Typography
              variant="body2"
              color={
                passwordPolicy.requireNonAlphanumeric
                  ? 'success.main'
                  : 'text.secondary'
              }
            >
              {passwordPolicy.requireNonAlphanumeric ? '✓' : '•'} Al menos un
              carácter especial
            </Typography>
            <Typography
              variant="body2"
              color={
                passwordPolicy.requiredUniqueChars
                  ? 'success.main'
                  : 'text.secondary'
              }
            >
              {passwordPolicy.requiredUniqueChars ? '✓' : '•'} Al menos 2
              caracteres únicos
            </Typography>
          </Stack>

          <TextField
            fullWidth
            label="Confirmar nueva contraseña"
            name="confirmPassword"
            type="password"
            value={form.confirmPassword}
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
              {submitting ? 'Guardando...' : 'Actualizar contraseña'}
            </Button>
          </Stack>
        </Stack>
      </CardContent>
    </Card>
  );
};

export default UpdatePassword;
