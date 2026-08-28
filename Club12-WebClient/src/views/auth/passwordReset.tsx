import { AxiosError } from 'axios';
import { useMemo, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import {
  Box,
  Button,
  Card,
  CardContent,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { authService } from '@/modules/auth/service/auth.service';
import { useError } from '@/modules/error/hooks/error.hock';
import InvalidToken from '@/views/core/errors/invalidToken';
import PasswordPolicyChecklist from '@/views/auth/PasswordPolicyChecklist';
import { buildPasswordPolicyMessages } from '@/modules/auth/utils/passwordPolicy';
import { HttpStatus } from '@/modules/core/constants/httpStatus';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';

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

  const hasRequiredParams = email.length > 0 && token.length > 0;

  const handleSubmit = async () => {
    const messages = buildPasswordPolicyMessages(newPassword, confirmPassword);

    if (!hasRequiredParams) {
      messages.unshift('El enlace de recuperación no es válido.');
    }

    if (messages.length > 0) {
      setMessage(HttpStatus.BadRequest, messages);
      return;
    }

    setSubmitting(true);
    try {
      const response = await authService.confirmPasswordResetRequest({
        email,
        token,
        newPassword,
      });

      if (response?.status === HttpStatus.Ok) {
        setMessage(HttpStatus.Ok, [
          'Contraseña actualizada correctamente. Iniciá sesión.',
        ]);
        navigate(APP_ROUTES.login, { replace: true });
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
      sx={{
        display: "flex",
        justifyContent: "center",
        alignItems: "center",
        minHeight: "90vh"
      }}>
      <Card sx={{ maxWidth: 520, width: '100%' }}>
        <CardContent>
          <Typography variant="h5" sx={{
            mb: 2
          }}>
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

            <PasswordPolicyChecklist password={newPassword} />

            <TextField
              fullWidth
              label="Confirmar nueva contraseña"
              type="password"
              value={confirmPassword}
              onChange={e => setConfirmPassword(e.target.value)}
            />

            <Stack direction="row" spacing={2} sx={{
              justifyContent: "flex-end"
            }}>
              <Button
                variant="outlined"
                onClick={() => navigate(APP_ROUTES.login, { replace: true })}
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
