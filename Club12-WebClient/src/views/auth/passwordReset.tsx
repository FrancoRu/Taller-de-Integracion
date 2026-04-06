import { AxiosError } from 'axios';
import { useMemo, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import {
  Box,
  Button,
  Card,
  CardContent,
  Divider,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { authService } from '@/modules/auth/service/auth.service';
import { useError } from '@/modules/error/hooks/error.hock';
import InvalidToken from '@/views/core/errors/invalidToken';

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

export default function PasswordReset() {
  const navigate = useNavigate();
  const location = useLocation();
  const { setError, setMessage } = useError();

  const normalizedSearchParams = useMemo(() => {
    const normalizedSearch = location.search.replace(/&amp;/gi, '&');
    return new URLSearchParams(normalizedSearch);
  }, [location.search]);

  const readQueryParam = (key: string) =>
    normalizedSearchParams.get(key)?.trim() ??
    normalizedSearchParams.get(`amp;${key}`)?.trim() ??
    '';

  const email = readQueryParam('email');
  const token = readQueryParam('token');

  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const passwordPolicy = useMemo(
    () => getPasswordPolicyState(newPassword),
    [newPassword]
  );

  const hasRequiredParams = email.length > 0 && token.length > 0;

  const handleSubmit = async () => {
    const messages: string[] = [];

    if (!hasRequiredParams) {
      messages.push('El enlace de recuperación no es válido.');
    }

    if (!newPassword) {
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

    if (!confirmPassword) {
      messages.push('La confirmación de contraseña es obligatoria.');
    }

    if (newPassword && confirmPassword && newPassword !== confirmPassword) {
      messages.push('La confirmación no coincide con la nueva contraseña.');
    }

    if (messages.length > 0) {
      setMessage(400, messages);
      return;
    }

    setSubmitting(true);
    try {
      const response = await authService.confirmPasswordResetRequest({
        email,
        token,
        newPassword,
      });

      if (response?.status === 200) {
        setMessage(200, [
          'Contraseña actualizada correctamente. Iniciá sesión.',
        ]);
        navigate('/login', { replace: true });
      }
    } catch (error: unknown) {
      setError(error as AxiosError);
    } finally {
      setSubmitting(false);
    }
  };

  if (!hasRequiredParams) {
    return <InvalidToken />;
  }

  return (
    <Box
      display="flex"
      justifyContent="center"
      alignItems="center"
      minHeight="90vh"
    >
      <Card sx={{ maxWidth: 520, width: '100%' }}>
        <CardContent>
          <Typography variant="h5" mb={2}>
            Restablecer contraseña
          </Typography>

          <Stack spacing={2}>
            <TextField fullWidth label="Email" value={email} disabled />

            <TextField
              fullWidth
              label="Nueva contraseña"
              type="password"
              value={newPassword}
              onChange={e => setNewPassword(e.target.value)}
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
                  passwordPolicy.requireDigit
                    ? 'success.main'
                    : 'text.secondary'
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
              type="password"
              value={confirmPassword}
              onChange={e => setConfirmPassword(e.target.value)}
            />

            <Stack direction="row" spacing={2} justifyContent="flex-end">
              <Button
                variant="outlined"
                onClick={() => navigate('/login', { replace: true })}
                disabled={submitting}
              >
                Cancelar
              </Button>
              <Button
                variant="contained"
                onClick={handleSubmit}
                disabled={submitting}
              >
                {submitting ? 'Guardando...' : 'Cambiar contraseña'}
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>
    </Box>
  );
}
