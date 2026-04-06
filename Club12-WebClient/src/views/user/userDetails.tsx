import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Box,
  Card,
  CardContent,
  CircularProgress,
  Divider,
  IconButton,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material';
import {
  ArrowBackIcon,
  EditIcon,
  LockResetIcon,
} from '@/views/core/MUI/icons/icons';
import { useAuth } from '@/modules/auth/hook/auth.hook';
import { useError } from '@/modules/error/hooks/error.hock';
import { useUser } from '@/modules/user/hook/user.hook';
import { UserRolesType } from '@/modules/core/enum/user/userRolesType';
import { GUID } from '@/modules/core/types/types';

const ROLE_LABELS: Record<UserRolesType, string> = {
  ADMIN: 'Admin',
  OWNER: 'Owner',
  TOURNAMENT_MANAGER: 'Responsable del Torneo',
  TEAM_MANAGER: 'Responsable de Equipo',
  GUEST: 'Invitado',
};

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
      setMessage(200, ['Password blanqueado correctamente.']);
    }
  };

  useEffect(() => {
    if (!userId) return;
    (async () => {
      setLoading(true);
      await getById(userId as GUID);
      setLoading(false);
    })();
  }, [userId]);

  return (
    <Card sx={{ maxWidth: 600, mx: 'auto', mt: 3 }}>
      <CardContent>
        <Stack
          direction="row"
          alignItems="center"
          justifyContent="space-between"
          mb={2}
        >
          <Stack direction="row" alignItems="center" spacing={1}>
            <Tooltip title="Volver">
              <IconButton
                size="small"
                onClick={() => navigate('/panel/usuarios')}
              >
                <ArrowBackIcon fontSize="small" />
              </IconButton>
            </Tooltip>
            <Typography variant="h6">Detalle de usuario</Typography>
          </Stack>

          {!loading && user && (
            <Stack direction="row" spacing={1}>
              {canResetPassword && (
                <Tooltip title="Blanquear password">
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
                  onClick={() => navigate(`/panel/usuarios/${userId}/editar`)}
                >
                  <EditIcon />
                </IconButton>
              </Tooltip>
            </Stack>
          )}
        </Stack>

        <Divider sx={{ mb: 2 }} />

        {loading ? (
          <Box display="flex" justifyContent="center" py={4}>
            <CircularProgress />
          </Box>
        ) : !user ? (
          <Typography color="text.secondary">
            No se encontró el usuario.
          </Typography>
        ) : (
          <Stack spacing={2}>
            <Field label="Usuario" value={user.username} />
            <Field label="Email" value={user.email} />
            <Field label="Teléfono" value={user.phoneNumber ?? '—'} />
            <Field
              label="Rol"
              value={ROLE_LABELS[user.role as UserRolesType] ?? user.role}
            />
          </Stack>
        )}
      </CardContent>
    </Card>
  );
};

const Field: React.FC<{ label: string; value: string }> = ({
  label,
  value,
}) => (
  <Box>
    <Typography variant="caption" color="text.secondary">
      {label}
    </Typography>
    <Typography variant="body1">{value}</Typography>
  </Box>
);

export default UserDetails;
