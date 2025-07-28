import { GUID } from '@/modules/core/types/types';
import { useTeam } from '@/modules/team/hook/team.hook';
import { ITeamResponse } from '@/modules/team/type/team';
import { EditIcon, DeleteIcon } from '@/views/core/MUI/icons/icons';
import {
  Card,
  CardContent,
  Stack,
  Typography,
  Tooltip,
  IconButton,
} from '@mui/material';
import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { DeleteTeam } from './delete-team';

export const DetailTeam: React.FC = () => {
  const { id } = useParams<{ id: GUID }>();
  const { team, getTeamById } = useTeam();
  if (!id) {
    return null;
  }

  useEffect(() => {
    (async () => {
      await getTeamById(id);
    })();
  }, []);

  if (!team) {
    return null;
  }
  return (
    <>
      <RenderTeamDetails {...team} />
    </>
  );
};

const RenderTeamDetails: React.FC<ITeamResponse> = ({
  id,
  name,
  threeLetterCode,
  shirtColor,
  logoUrl,
}) => {
  const navigate = useNavigate();
  const [showPopupDelete, setShowPopupDelete] = useState<boolean>(false);
  const { deleteTeamById } = useTeam();

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
            Equipo: {name}
          </Typography>

          <Stack direction="row" spacing={1}>
            <Tooltip title="Editar Equipo">
              <IconButton color="primary" onClick={() => navigate(`editar`)}>
                <EditIcon />
              </IconButton>
            </Tooltip>

            <Tooltip title="Eliminar Equipo">
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
          Código: {threeLetterCode}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Color de camiseta: {shirtColor}
        </Typography>

        {logoUrl && (
          <Typography
            variant="body2"
            color="primary"
            sx={{ mt: 1, wordBreak: 'break-word' }}
          >
            Logo: {logoUrl}
          </Typography>
        )}
      </CardContent>

      {showPopupDelete && (
        <DeleteTeam
          id={id}
          fn={deleteTeamById}
          onClose={() => setShowPopupDelete(false)}
        />
      )}
    </Card>
  );
};
