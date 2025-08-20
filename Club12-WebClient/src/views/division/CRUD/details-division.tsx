import { GUID } from '@/modules/core/types/types';
import { useDivision } from '@/modules/division/hook/division.hook';
import {
  IDivisionContextProps,
  IDivisionResponse,
} from '@/modules/division/type/division';
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
import { Outlet, useNavigate, useParams } from 'react-router-dom';
import { DeleteDivision } from './delete-division';
import { InfoStage } from '@/views/stage/info';

export const DetailDidivion: React.FC = () => {
  const { divisionId: id } = useParams<{ divisionId: GUID }>();
  const { division, getDivisionsById } = useDivision();
  if (!id) {
    return null;
  }

  useEffect(() => {
    (async () => {
      await getDivisionsById(id);
    })();
  }, []);

  if (!division) {
    return null;
  }

  return (
    <>
      <RenderDivisionDetails {...division} />
      <InfoStage {...division} />
      <Outlet />
    </>
  );
};

const RenderDivisionDetails: React.FC<IDivisionResponse> = ({ id, name }) => {
  const navigate = useNavigate();
  const [showPopupDelete, setShowPopupDelete] = useState<boolean>(false);
  const { deleteDivisionsById }: IDivisionContextProps = useDivision();
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
            División: {name}
          </Typography>

          <Stack direction="row" spacing={1}>
            <Tooltip title="Editar División">
              <IconButton color="primary" onClick={() => navigate(`editar`)}>
                <EditIcon />
              </IconButton>
            </Tooltip>

            <Tooltip title="Eliminar División">
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
        <DeleteDivision
          id={id}
          fn={deleteDivisionsById}
          onClose={() => setShowPopupDelete(false)}
        />
      )}
    </Card>
  );
};
