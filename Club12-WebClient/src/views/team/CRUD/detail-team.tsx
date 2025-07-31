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
  Box,
} from '@mui/material';
import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { DeleteTeam } from './delete-team';
import { RoutesNavigationViews } from '@/views/core/routes-const';
import { InfoPlayer } from '@/views/player/info';

export const DetailTeam: React.FC = () => {
  const { id } = useParams<{ id: GUID }>();
  const { team, getTeamById } = useTeam();

  useEffect(() => {
    (async () => {
      await getTeamById(id);
    })();
  }, [id]);

  return (
    <>
      {team && (
        <>
          <RenderTeamDetails {...team} />
          <InfoPlayer {...team} />
        </>
      )}
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

        <Stack
          direction="row"
          spacing={2}
          alignItems="center"
          justifyContent="space-between"
        >
          {logoUrl && (
            <Box
              sx={{
                width: 80,
                height: 80,
                borderRadius: '50%',
                overflow: 'hidden',
                border: '2px solid orange',
                boxShadow: '0 0 8px rgba(255,165,0,0.7)',
                display: { xs: 'none', sm: 'block' },
                flexShrink: 0,
              }}
            >
              <img
                src={logoUrl}
                alt={`Logo del equipo ${name}`}
                style={{ width: '100%', height: '100%', objectFit: 'cover' }}
              />
            </Box>
          )}

          <Box sx={{ flexGrow: 1 }}>
            <Typography variant="body2" color="text.secondary" gutterBottom>
              Código: {threeLetterCode}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Color de camiseta: {shirtColor}
            </Typography>
          </Box>
        </Stack>
      </CardContent>

      {showPopupDelete && (
        <DeleteTeam
          id={id}
          route={RoutesNavigationViews.Team}
          fn={deleteTeamById}
          onClose={() => setShowPopupDelete(false)}
        />
      )}
    </Card>
  );
};
