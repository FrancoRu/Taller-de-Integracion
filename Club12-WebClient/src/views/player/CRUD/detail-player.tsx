import { GUID } from '@/modules/core/types/types';
import { usePlayer } from '@/modules/player/hook/player.hook';
import { IPlayerResponse } from '@/modules/player/type/player';
import { EditIcon, DeleteIcon } from '@/views/core/MUI/icons/icons';
import {
  Card,
  CardContent,
  IconButton,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material';
import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { DeletePlayer } from './delete-player';
import { useTeam } from '@/modules/team/hook/team.hook';

export const DetailPlayer: React.FC = () => {
  const { id } = useParams<{ id: GUID }>();
  const { player, getPlayerById } = usePlayer();

  const { team, getTeamById } = useTeam();
  if (!id) return null;

  useEffect(() => {
    (async () => {
      await getPlayerById(id);
      await getTeamById(player?.teamId);
    })();
  }, [id]);

  if (!player) return null;

  return (
    <>
      <RenderPlayerDetails {...player} teamName={team?.name ?? ''} />
    </>
  );
};

const RenderPlayerDetails: React.FC<IPlayerResponse & { teamName: string }> = ({
  id,
  firstName,
  secondName,
  lastName,
  documentNumber,
  birthDate,
  phoneNumber,
  socialSecurity,
  teamName,
}) => {
  const navigate = useNavigate();
  const [showPopupDelete, setShowPopupDelete] = useState<boolean>(false);
  const { deletePlayerById } = usePlayer();
  return (
    <Card
      sx={{
        width: '98%',
        mx: 'auto',
        px: { xs: 2, sm: 3, md: 4 },
      }}
    >
      <CardContent>
        <Stack
          direction="row"
          justifyContent="space-between"
          alignItems="center"
          mb={1}
        >
          <Typography variant="h6" fontWeight="bold">
            Jugador:{' '}
            {!secondName?.trim()
              ? `${firstName} ${lastName}`
              : `${firstName} ${secondName} ${lastName}`}
          </Typography>

          <Stack direction="row" spacing={1}>
            <Tooltip title="Editar Jugador">
              <IconButton color="primary" onClick={() => navigate(`editar`)}>
                <EditIcon />
              </IconButton>
            </Tooltip>

            <Tooltip title="Eliminar Jugador">
              <IconButton
                color="error"
                onClick={() => setShowPopupDelete(true)}
              >
                <DeleteIcon />
              </IconButton>
            </Tooltip>
          </Stack>
        </Stack>

        <Typography variant="body2" color="text.secondary">
          Documento: {documentNumber}
        </Typography>
        {birthDate && (
          <Typography variant="body2" color="text.secondary">
            Fecha de Nacimiento: {new Date(birthDate).toLocaleDateString()}
          </Typography>
        )}
        {phoneNumber && (
          <Typography variant="body2" color="text.secondary">
            Teléfono: {phoneNumber}
          </Typography>
        )}
        {socialSecurity && (
          <Typography variant="body2" color="text.secondary">
            Obra Social: {socialSecurity}
          </Typography>
        )}
        <Typography variant="body2" color="text.secondary">
          Nombre de Equipo: {teamName}
        </Typography>
      </CardContent>

      {showPopupDelete && (
        <DeletePlayer
          id={id}
          fn={deletePlayerById}
          onClose={() => setShowPopupDelete(false)}
        />
      )}
    </Card>
  );
};
