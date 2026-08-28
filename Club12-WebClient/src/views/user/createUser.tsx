import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Button,
  Card,
  CardContent,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useAuth } from '@/modules/auth/hook/auth.hook';
import { useUser } from '@/modules/user/hook/user.hook';
import { RegisterUserRequest } from '@/modules/user/type/user';
import {
  USER_ROLE_LABELS,
  UserRolesType,
} from '@/modules/core/enum/user/userRolesType';
import { useError } from '@/modules/error/hooks/error.hock';
import { HttpStatus } from '@/modules/core/constants/httpStatus';
import { USERNAME_LENGTH } from '@/modules/core/constants/constants';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';

/**
 * Roles each caller may create, mirroring the backend's account-creation
 * policy in IdentityAuthenticationService exactly: ADMIN and OWNER are
 * both super-admin roles and may create an account with any role.
 */
const SUPER_ADMIN_ASSIGNABLE_ROLES: UserRolesType[] = [
  UserRolesType.Admin,
  UserRolesType.Owner,
];

const EMPTY_FORM: RegisterUserRequest = {
  email: '',
  username: '',
  phone: '',
  role: '',
};

const CreateUser: React.FC = () => {
  const navigate = useNavigate();
  const { role: loggedRole } = useAuth();
  const { createUser } = useUser();
  const { errors, setMessage } = useError();
  const [submitting, setSubmitting] = useState(false);

  const isAdmin = loggedRole === UserRolesType.Admin;
  const isOwner = loggedRole === UserRolesType.Owner;

  useEffect(() => {
    if (!isAdmin && !isOwner) {
      navigate(APP_ROUTES.home, { replace: true });
    }
  }, [isAdmin, isOwner, navigate]);

  const [form, setForm] = useState<RegisterUserRequest>({
    ...EMPTY_FORM,
  });

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    setForm(prev => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async () => {
    const messages: string[] = [];

    if (!form.email.trim()) messages.push('El email es requerido.');
    if (!form.username.trim())
      messages.push('El nombre de usuario es requerido.');
    if (
      form.username.length > 0 &&
      (form.username.length < 3 || form.username.length > 50)
    )
      messages.push('El nombre de usuario debe tener entre 3 y 50 caracteres.');
    if (!form.role) messages.push('El rol es requerido.');

    if (messages.length > 0) {
      setMessage(HttpStatus.BadRequest, messages);
      return;
    }

    const payload: RegisterUserRequest = {
      email: form.email.trim(),
      username: form.username.trim(),
      phone: form.phone?.trim() || undefined,
      role: form.role,
    };

    setSubmitting(true);
    const result = await createUser(payload);
    setSubmitting(false);

    if (result) {
      navigate(APP_ROUTES.panelUsers);
    }
  };

  if (!isAdmin && !isOwner) return null;

  return (
    <Card sx={{ maxWidth: 520, mx: 'auto', mt: 3 }}>
      <CardContent>
        <Typography variant="h6" sx={{
          mb: 2
        }}>
          Registrar nuevo usuario
        </Typography>

        {errors && errors.length > 0 && (
          <Stack spacing={0.5} sx={{
            mb: 2
          }}>
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
            label="Email"
            name="email"
            type="email"
            value={form.email}
            onChange={handleChange}
          />

          <TextField
            fullWidth
            label="Nombre de usuario"
            name="username"
            value={form.username}
            onChange={handleChange}
            slotProps={{
              htmlInput: {
                minLength: USERNAME_LENGTH.Min,
                maxLength: USERNAME_LENGTH.Max,
              }
            }}
          />

          <TextField
            fullWidth
            label="Teléfono (opcional)"
            name="phone"
            value={form.phone ?? ''}
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
            {SUPER_ADMIN_ASSIGNABLE_ROLES.map(
              r => (
                <MenuItem key={r} value={r}>
                  {USER_ROLE_LABELS[r]}
                </MenuItem>
              )
            )}
          </TextField>

          <Stack direction="row" spacing={2} sx={{
            justifyContent: "flex-end"
          }}>
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
              {submitting ? 'Guardando...' : 'Crear usuario'}
            </Button>
          </Stack>
        </Stack>
      </CardContent>
    </Card>
  );
};

export default CreateUser;
