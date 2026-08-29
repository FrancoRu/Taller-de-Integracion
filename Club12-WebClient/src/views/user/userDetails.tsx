import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Box, IconButton, Stack, Tooltip, Typography } from '@mui/material';
import { EditIcon, LockResetIcon } from '@/views/core/MUI/icons/icons';
import PageShell from '@/views/core/components/PageShell';
import { DetailSkeleton } from '@/views/core/components/skeletons';
import { useAuth } from '@/modules/auth/hook/auth.hook';
import { useError } from '@/modules/error/hooks/error.hock';
import { useUser } from '@/modules/user/hook/user.hook';
import {
  UserRolesType,
  USER_ROLE_LABELS,
} from '@/modules/core/enum/user/userRolesType';
import { GUID } from '@/modules/core/types/types';
import { HttpStatus } from '@/modules/core/constants/httpStatus';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';

const UserDetails: React.FC = () => {
  const { userId } = useParams<{ userId: string }>();
  const navigate = useNavigate();
  const { role } = useAuth();
  const { setMessage } = useError();
  const { user, getById, resetUserPassword } = useUser();
  const [loading, setLoading] = useState(true);
  const [resetting, setResetting] = useState(false);
  const canResetPassword =
    role === UserRolesType.Admin || role === UserRolesType.Owner;

  const handlePasswordReset = async () => {
    if (!userId || resetting) {
      return;
    }

    setResetting(true);
    const ok = await resetUserPassword(userId as GUID);
    setResetting(false);

    if (ok) {
      setMessage(HttpStatus.Ok, ['Password blanqueado correctamente.']);
    }
  };

  useEffect(() => {
    if (!userId) return;
    (async () => {
      setLoading(true);
      await getById(userId as GUID);
      setLoading(false);
    })();
  }, [userId, getById]);

  return (
    <PageShell
      title="Detalle de usuario"
      maxWidth="sm"
      back={{ label: 'Volver', onClick: () => navigate(APP_ROUTES.panelUsers) }}
      actions={
        !loading && user ? (
          <>
            {canResetPassword && (
              <Tooltip title="Blanquear contraseña">
                <IconButton
                  color="warning"
                  onClick={handlePasswordReset}
                  disabled={resetting}
                >
                  <LockResetIcon />
                </IconButton>
              </Tooltip>
            )}
            <Tooltip title="Editar">
              <IconButton
                color="primary"
                onClick={() =>
                  navigate(APP_ROUTES.panelUserEdit.build(userId as string))
                }
              >
                <EditIcon />
              </IconButton>
            </Tooltip>
          </>
        ) : undefined
      }
    >
      {loading ? (
        <DetailSkeleton />
      ) : !user ? (
        <Typography sx={{ color: 'text.secondary' }}>
          No se encontró el usuario.
        </Typography>
      ) : (
        <Stack spacing={2}>
          <Field label="Usuario" value={user.username} />
          <Field label="Email" value={user.email} />
          <Field label="Teléfono" value={user.phoneNumber ?? '—'} />
          <Field
            label="Rol"
            value={USER_ROLE_LABELS[user.role as UserRolesType] ?? user.role}
          />
        </Stack>
      )}
    </PageShell>
  );
};

const Field: React.FC<{ label: string; value: string }> = ({
  label,
  value,
}) => (
  <Box>
    <Typography variant="caption" sx={{
      color: "text.secondary"
    }}>
      {label}
    </Typography>
    <Typography variant="body1">{value}</Typography>
  </Box>
);

export default UserDetails;
