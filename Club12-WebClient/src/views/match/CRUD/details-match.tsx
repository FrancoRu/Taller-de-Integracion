import { GUID } from '@/modules/core/types/types';
import { useMatch } from '@/modules/match/hook/match.hook';
import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { IMatchContextProps, IMatchResponse } from '@/modules/match/type/match';
import { EditIcon, DeleteIcon } from '@/views/core/MUI/icons/icons';
import {
  Card,
  CardContent,
  Stack,
  Typography,
  Tooltip,
  IconButton,
} from '@mui/material';
import { DeleteMatch } from './delete-match';

export const DetailMatch: React.FC = () => {
  const { id } = useParams<{ id: GUID }>();
  const { match, getMatchById } = useMatch();
  if (!id) {
    return null;
  }

  useEffect(() => {
    (async () => {
      await getMatchById(id);
    })();
  }, []);

  if (!match) {
    return null;
  }
  return (
    <>
      <RenderMatchDetails {...match} />
    </>
  );
};

const RenderMatchDetails: React.FC<IMatchResponse> = ({
  id,
  homeTeamName,
  visitorTeamName,
}) => {
  const navigate = useNavigate();
  const [showPopupDelete, setShowPopupDelete] = useState<boolean>(false);
  const { deleteMatchById }: IMatchContextProps = useMatch();
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
            Partido: {homeTeamName} - {visitorTeamName}
          </Typography>

          <Stack direction="row" spacing={1}>
            <Tooltip title="Editar Etapa">
              <IconButton color="primary" onClick={() => navigate(`editar`)}>
                <EditIcon />
              </IconButton>
            </Tooltip>

            <Tooltip title="Eliminar Etapa">
              <IconButton
                color="error"
                onClick={() => setShowPopupDelete(true)}
              >
                <DeleteIcon />
              </IconButton>
            </Tooltip>
          </Stack>
        </Stack>
      </CardContent>

      {showPopupDelete && (
        <DeleteMatch
          id={id}
          fn={deleteMatchById}
          onClose={() => setShowPopupDelete(false)}
        />
      )}
    </Card>
  );
};
