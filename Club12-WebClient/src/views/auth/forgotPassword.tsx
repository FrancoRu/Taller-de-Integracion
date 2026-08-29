import { AxiosError } from 'axios';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
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
import { HttpStatus } from '@/modules/core/constants/httpStatus';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import {
  isValidEmail,
  VALIDATION_MESSAGES,
} from '@/modules/core/utils/validators';

/**
 * HU-10: self-service "Olvidé mi contraseña" screen. Posts the email to
 * POST /api/auth/password-reset/request, which always succeeds (no account
 * enumeration) and emails a magic reset link consumed by PasswordReset.
 */
export default function ForgotPassword() {
  const navigate = useNavigate();
  const { setError, setMessage } = useError();

  const [email, setEmail] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [sent, setSent] = useState(false);

  const emailError = email.length > 0 && !isValidEmail(email);

  const handleSubmit = async () => {
    if (!email.trim()) {
      setMessage(HttpStatus.BadRequest, ['El email es obligatorio.']);
      return;
    }

    if (!isValidEmail(email)) {
      setMessage(HttpStatus.BadRequest, [VALIDATION_MESSAGES.email + '.']);
      return;
    }

    setSubmitting(true);
    try {
      const response = await authService.requestPasswordResetRequest({
        email: email.trim(),
      });

      if (response?.status === HttpStatus.Ok) {
        setSent(true);
        setMessage(HttpStatus.Ok, [
          'Si el email existe, te enviamos un link para restablecer tu contraseña.',
        ]);
      }
    } catch (error: unknown) {
      setError(error as AxiosError);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Box
      sx={{
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        minHeight: '90vh',
      }}
    >
      <Card sx={{ maxWidth: 440, width: '100%' }}>
        <CardContent>
          <Typography variant="h5" sx={{ mb: 1 }}>
            Olvidé mi contraseña
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Ingresá tu email y te enviaremos un link para restablecerla.
          </Typography>

          <Stack spacing={2}>
            <TextField
              fullWidth
              label="Email"
              type="email"
              value={email}
              onChange={e => setEmail(e.target.value)}
              disabled={sent}
              error={emailError}
              helperText={emailError ? VALIDATION_MESSAGES.email : undefined}
              onKeyDown={e => {
                if (e.key === 'Enter') {
                  void handleSubmit();
                }
              }}
            />

            <Stack
              direction="row"
              spacing={2}
              sx={{ justifyContent: 'flex-end' }}
            >
              <Button
                variant="outlined"
                onClick={() => navigate(APP_ROUTES.login, { replace: true })}
                disabled={submitting}
              >
                Volver
              </Button>
              <Button
                variant="contained"
                onClick={handleSubmit}
                disabled={submitting || sent || emailError}
              >
                {submitting ? 'Enviando...' : 'Enviar link'}
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>
    </Box>
  );
}
