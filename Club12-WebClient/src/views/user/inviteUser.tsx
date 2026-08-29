import { AxiosError } from 'axios';
import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, MenuItem, Stack, TextField, Typography } from '@mui/material';
import PageShell from '@/views/core/components/PageShell';
import { useAuth } from '@/modules/auth/hook/auth.hook';
import { authService } from '@/modules/auth/service/auth.service';
import { InviteUserRequest } from '@/modules/auth/type/auth';
import {
  USER_ROLE_LABELS,
  UserRolesType,
} from '@/modules/core/enum/user/userRolesType';
import { useError } from '@/modules/error/hooks/error.hock';
import { HttpStatus } from '@/modules/core/constants/httpStatus';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';

/**
 * HU-09: "Invitar usuario" form (Admin/Owner). Creates a passwordless account
 * by email + role and triggers the magic activation link — the invited user
 * sets their own password from the email (see ActivateAccount).
 */
const ASSIGNABLE_ROLES: UserRolesType[] = [
  UserRolesType.Admin,
  UserRolesType.Owner,
];

interface InviteForm {
  email: string;
  phone: string;
  role: string;
}

const EMPTY_FORM: InviteForm = {
  email: '',
  phone: '',
  role: '',
};

const InviteUser: React.FC = () => {
  const navigate = useNavigate();
  const { role: loggedRole } = useAuth();
  const { setError, setMessage } = useError();
  const [submitting, setSubmitting] = useState(false);
  const [form, setForm] = useState<InviteForm>({ ...EMPTY_FORM });

  const isAdmin = loggedRole === UserRolesType.Admin;
  const isOwner = loggedRole === UserRolesType.Owner;

  useEffect(() => {
    if (!isAdmin && !isOwner) {
      navigate(APP_ROUTES.home, { replace: true });
    }
  }, [isAdmin, isOwner, navigate]);

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    setForm(prev => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async () => {
    const messages: string[] = [];
    if (!form.email.trim()) messages.push('El email es requerido.');
    if (!form.role) messages.push('El rol es requerido.');

    if (messages.length > 0) {
      setMessage(HttpStatus.BadRequest, messages);
      return;
    }

    const payload: InviteUserRequest = {
      email: form.email.trim(),
      phone: form.phone.trim() || undefined,
      role: form.role,
    };

    setSubmitting(true);
    try {
      const response = await authService.inviteRequest(payload);
      if (
        response?.status === HttpStatus.Created ||
        response?.status === HttpStatus.Ok
      ) {
        setMessage(HttpStatus.Ok, [
          `Se envió un link de activación a ${payload.email}.`,
        ]);
        navigate(APP_ROUTES.panelUsers);
      }
    } catch (error: unknown) {
      setError(error as AxiosError);
    } finally {
      setSubmitting(false);
    }
  };

  if (!isAdmin && !isOwner) return null;

  return (
    <PageShell
      title="Invitar usuario"
      maxWidth="sm"
      back={{ label: 'Volver', onClick: () => navigate(APP_ROUTES.panelUsers) }}
    >
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Se crea la cuenta sin contraseña y se le envía un link de activación
          por email para que la defina.
        </Typography>

        <Stack spacing={2}>
          <TextField
            fullWidth
            label="Email"
            name="email"
            type="email"
            value={form.email}
            onChange={handleChange}
          />

          <TextField
            fullWidth
            label="Teléfono (opcional)"
            name="phone"
            value={form.phone}
            onChange={handleChange}
          />

          <TextField
            select
            fullWidth
            label="Rol"
            name="role"
            value={form.role}
            onChange={handleChange}
          >
            <MenuItem value="">Seleccionar rol</MenuItem>
            {ASSIGNABLE_ROLES.map(r => (
              <MenuItem key={r} value={r}>
                {USER_ROLE_LABELS[r]}
              </MenuItem>
            ))}
          </TextField>

          <Stack
            direction="row"
            spacing={2}
            sx={{ justifyContent: 'flex-end' }}
          >
            <Button
              variant="outlined"
              onClick={() => navigate(APP_ROUTES.panelUsers)}
              disabled={submitting}
            >
              Cancelar
            </Button>
            <Button
              variant="contained"
              onClick={handleSubmit}
              disabled={submitting}
            >
              {submitting ? 'Enviando...' : 'Enviar invitación'}
            </Button>
          </Stack>
        </Stack>
    </PageShell>
  );
};

export default InviteUser;
