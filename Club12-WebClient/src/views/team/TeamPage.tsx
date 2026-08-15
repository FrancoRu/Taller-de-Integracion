import React, { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Card,
  CardContent,
  Grid,
  Stack,
  Tab,
  Tabs,
  Typography,
} from '@mui/material';
import { GUID } from '@/modules/core/types/types';
import { useTeam } from '@/modules/team/hook/team.hook';
import LoadingIndicator from '@/views/core/components/LoadingIndicator';
import TeamLogo from '@/views/core/components/TeamLogo';
import PlayersPage from '@/views/player/PlayersPage';
import NewEntityButton from '@/views/core/components/NewEntityButton';
import Swal from 'sweetalert2';
import theme from '@/theme';

interface TeamPageProps {
  teamIdOverride?: GUID;
  wrapInCard?: boolean;
  hideBackLink?: boolean;
}

const TeamPage: React.FC<TeamPageProps> = ({
  teamIdOverride,
  wrapInCard = true,
  hideBackLink = false,
}) => {
  const { teamId } = useParams<{ teamId: GUID }>();
  const navigate = useNavigate();
  const { team, getTeamById } = useTeam();
  const [loading, setLoading] = useState(false);
  const [tab, setTab] = useState<
    'detalle' | 'jugadores' | 'puntuaciones' | 'sanciones'
  >('detalle');

  const targetTeamId = useMemo(
    () => teamIdOverride ?? teamId ?? team?.id,
    [team?.id, teamId, teamIdOverride]
  );

  useEffect(() => {
    if (!targetTeamId) {
      return;
    }

    const fetchTeam = async () => {
      setLoading(true);
      await getTeamById(targetTeamId);
      setLoading(false);
    };

    void fetchTeam();
  }, [getTeamById, targetTeamId]);

  if (!targetTeamId) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6">Equipo</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            No se recibió un equipo para visualizar.
          </Typography>
        </CardContent>
      </Card>
    );
  }

  if (loading) {
    return <LoadingIndicator />;
  }

  if (!team || team.id !== targetTeamId) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6">Equipo no encontrado</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            No fue posible cargar la información del equipo.
          </Typography>
          {!hideBackLink && (
            <Typography
              component="button"
              onClick={() => navigate('/panel/equipos')}
              sx={{
                mt: 2,
                border: 0,
                background: 'none',
                color: 'primary.main',
                cursor: 'pointer',
                p: 0,
              }}
            >
              Volver al listado
            </Typography>
          )}
        </CardContent>
      </Card>
    );
  }

  const handleCreateScore = () => {
    void Swal.fire({
      title: 'Pendiente',
      text: 'La creación de puntuaciones desde esta vista aún no está implementada.',
      icon: 'info',
      confirmButtonColor: theme.palette.primary.main,
    });
  };

  const handleCreateSanction = () => {
    void Swal.fire({
      title: 'Pendiente',
      text: 'La creación de sanciones desde esta vista aún no está implementada.',
      icon: 'info',
      confirmButtonColor: theme.palette.primary.main,
    });
  };

  const content = (
    <>
      <Stack direction="row" alignItems="center" spacing={1.5} mb={2}>
        <TeamLogo teamName={team.name} logoUrl={team.logoUrl} size={44} />
        <Typography variant="h6">{team.name}</Typography>
      </Stack>

      <Tabs
        value={tab}
        onChange={(_, value) => setTab(value)}
        sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}
      >
        <Tab label="Detalle" value="detalle" />
        <Tab label="Jugadores" value="jugadores" />
        <Tab label="Puntuaciones" value="puntuaciones" />
        <Tab label="Sanciones" value="sanciones" />
      </Tabs>

      {tab === 'detalle' && (
        <Grid container spacing={2}>
          <Grid item xs={12} md={6}>
            <Typography variant="subtitle2" color="text.secondary">
              Nombre
            </Typography>
            <Typography>{team.name}</Typography>
          </Grid>
          <Grid item xs={12} md={6}>
            <Typography variant="subtitle2" color="text.secondary">
              Código
            </Typography>
            <Typography>{team.threeLetterCode}</Typography>
          </Grid>
          <Grid item xs={12} md={6}>
            <Typography variant="subtitle2" color="text.secondary">
              Color camiseta
            </Typography>
            <Typography>{team.shirtColor || '—'}</Typography>
          </Grid>
          <Grid item xs={12} md={6}>
            <Typography variant="subtitle2" color="text.secondary">
              Jugadores registrados
            </Typography>
            <Typography>{team.players?.length ?? 0}</Typography>
          </Grid>
        </Grid>
      )}

      {tab === 'jugadores' && (
        <PlayersPage
          teamId={targetTeamId}
          title={undefined}
          emptyMessage="Este equipo no tiene jugadores cargados."
          wrapInCard={false}
        />
      )}

      {tab === 'puntuaciones' && (
        <>
          <Stack direction="row" justifyContent="flex-end" mb={2}>
            <NewEntityButton type="Puntuación" onClick={handleCreateScore} />
          </Stack>
          <Typography variant="body2" color="text.secondary">
            Próximamente: puntuaciones del equipo.
          </Typography>
        </>
      )}

      {tab === 'sanciones' && (
        <>
          <Stack direction="row" justifyContent="flex-end" mb={2}>
            <NewEntityButton type="Sanción" onClick={handleCreateSanction} />
          </Stack>
          <Typography variant="body2" color="text.secondary">
            Próximamente: sanciones del equipo.
          </Typography>
        </>
      )}
    </>
  );

  if (wrapInCard) {
    return (
      <Card>
        <CardContent>{content}</CardContent>
      </Card>
    );
  }

  return content;
};

export default TeamPage;
