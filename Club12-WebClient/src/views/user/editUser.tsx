import React, { useEffect, useState } from 'react';
import Cookies from 'js-cookie';
import { decodeToken } from 'react-jwt';
import { useNavigate, useParams } from 'react-router-dom';
import { Button, MenuItem, Stack, TextField, Typography } from '@mui/material';
import PageShell from '@/views/core/components/PageShell';
import { DetailSkeleton } from '@/views/core/components/skeletons';
import { useAuth } from '@/modules/auth/hook/auth.hook';
import { useUser } from '@/modules/user/hook/user.hook';
import { GUID } from '@/modules/core/types/types';
import { UpdateUserRequest } from '@/modules/user/type/user';
import {
  USER_ROLE_LABELS,
  UserRolesType,
} from '@/modules/core/enum/user/userRolesType';
import { useError } from '@/modules/error/hooks/error.hock';
import {
  COOKIE_SIGNIN_TOKEN,
  USERNAME_LENGTH,
} from '@/modules/core/constants/constants';
import { HttpStatus } from '@/modules/core/constants/httpStatus';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import {
  isValidEmail,
  isValidPhone,
  VALIDATION_MESSAGES,
} from '@/modules/core/utils/validators';

/**
 * Roles each caller may assign via the edit form, mirroring the backend's
 * role-change policy in IdentityUserManagementService exactly: ADMIN and
 * OWNER are both super-admin roles and may set any role. The backend
 * re-enforces this regardless of what the form sends — this only keeps the
 * UI from offering choices that would be rejected anyway.
 */
const SUPER_ADMIN_ASSIGNABLE_ROLES: UserRolesType[] = [
  UserRolesType.Admin,
  UserRolesType.Owner,
];

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

const EditUser: React.FC = () => {
  const { userId: routeUserId } = useParams<{ userId: string }>();
  const navigate = useNavigate();
  const { role: loggedRole } = useAuth();
  const { user, getById, updateUser } = useUser();
  const { errors, setMessage } = useError();
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const targetUserId =
    routeUserId && GUID_REGEX.test(routeUserId)
      ? (routeUserId as GUID)
      : getUserIdFromToken();
  const isSelfProfileMode = !routeUserId;

  const isAdmin = loggedRole === UserRolesType.Admin;
  const isOwner = loggedRole === UserRolesType.Owner;
  // A caller can never change their own role (enforced server-side too), so
  // the field is only offered when editing someone else's account.
  const canChangeRole = !isSelfProfileMode && (isAdmin || isOwner);
  const assignableRoles = SUPER_ADMIN_ASSIGNABLE_ROLES;

  const [form, setForm] = useState<UpdateUserRequest>({
    username: '',
    email: '',
    phone: '',
  });
  const [initialRole, setInitialRole] = useState<UserRolesType | undefined>();

  useEffect(() => {
    if (!targetUserId) {
      navigate(APP_ROUTES.panel, { replace: true });
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
          role: data.role,
        });
        setInitialRole(data.role);
      }
      setLoading(false);
    })();
  }, [targetUserId, getById, navigate]);

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    setForm(prev => ({ ...prev, [name]: value }));
  };

  const emailValue = form.email ?? '';
  const phoneValue = form.phone ?? '';
  const emailError = emailValue.length > 0 && !isValidEmail(emailValue);
  const phoneError = phoneValue.length > 0 && !isValidPhone(phoneValue);

  const handleSubmit = async () => {
    if (!targetUserId) return;

    const messages: string[] = [];
    if (emailValue.trim() && !isValidEmail(emailValue))
      messages.push(VALIDATION_MESSAGES.email + '.');
    if (phoneValue.trim() && !isValidPhone(phoneValue))
      messages.push(VALIDATION_MESSAGES.phone + '.');

    if (messages.length > 0) {
      setMessage(HttpStatus.BadRequest, messages);
      return;
    }

    const roleChanged = canChangeRole && !!form.role && form.role !== initialRole;

    const payload: UpdateUserRequest = {
      username: form.username?.trim() || undefined,
      email: form.email?.trim() || undefined,
      phone: form.phone?.trim() || undefined,
      role: roleChanged ? form.role : undefined,
    };

    if (!payload.username && !payload.email && !payload.phone && !payload.role) {
      setMessage(HttpStatus.BadRequest, [
        'Debes completar al menos un campo para actualizar.',
      ]);
      return;
    }

    setSubmitting(true);
    const result = await updateUser(targetUserId, payload);
    setSubmitting(false);

    if (result) {
      if (isSelfProfileMode) {
        setMessage(HttpStatus.Ok, ['Perfil actualizado correctamente.']);
      } else {
        navigate(APP_ROUTES.panelUser.build(targetUserId));
      }
    }
  };

  const pageTitle = isSelfProfileMode ? 'Editar perfil' : 'Editar usuario';
  const handleBack = () => navigate(-1);

  if (loading) {
    return (
      <PageShell title={pageTitle} maxWidth="sm">
        <DetailSkeleton />
      </PageShell>
    );
  }

  if (!user) {
    return (
      <PageShell
        title={pageTitle}
        maxWidth="sm"
        back={{ label: 'Volver', onClick: handleBack }}
      >
        <Typography sx={{ color: 'text.secondary' }}>
          No se encontró el usuario.
        </Typography>
      </PageShell>
    );
  }

  return (
    <PageShell
      title={pageTitle}
      maxWidth="sm"
      back={{ label: 'Volver', onClick: handleBack }}
    >
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
            label="Nombre de usuario"
            name="username"
            value={form.username ?? ''}
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
            label="Email"
            name="email"
            type="email"
            value={form.email ?? ''}
            onChange={handleChange}
            error={emailError}
            helperText={emailError ? VALIDATION_MESSAGES.email : undefined}
          />

          <TextField
            fullWidth
            label="Teléfono"
            name="phone"
            value={form.phone ?? ''}
            onChange={handleChange}
            error={phoneError}
            helperText={phoneError ? VALIDATION_MESSAGES.phone : undefined}
          />

          {canChangeRole && (
            <TextField
              select
              fullWidth
              label="Rol"
              name="role"
              value={form.role ?? ''}
              onChange={handleChange}
            >
              {assignableRoles.map(r => (
                <MenuItem key={r} value={r}>
                  {USER_ROLE_LABELS[r]}
                </MenuItem>
              ))}
              {initialRole && !assignableRoles.includes(initialRole) && (
                <MenuItem value={initialRole}>
                  {USER_ROLE_LABELS[initialRole]}
                </MenuItem>
              )}
            </TextField>
          )}

          <Stack direction="row" spacing={2} sx={{
            justifyContent: "flex-end"
          }}>
            <Button
              variant="outlined"
              onClick={() =>
                navigate(
                  isSelfProfileMode
                    ? APP_ROUTES.panelEditProfile
                    : APP_ROUTES.panelUser.build(targetUserId as string)
                )
              }
              disabled={submitting}
            >
              Cancelar
            </Button>
            <Button
              variant="contained"
              onClick={handleSubmit}
              disabled={submitting || emailError || phoneError}
            >
              {submitting ? 'Guardando...' : 'Guardar cambios'}
            </Button>
          </Stack>
        </Stack>
    </PageShell>
  );
};

export default EditUser;
