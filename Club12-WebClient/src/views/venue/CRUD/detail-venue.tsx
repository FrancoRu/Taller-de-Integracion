import { useVenue } from '@/modules/venue/hook/venue.hook';
import { IVenueResponse } from '@/modules/venue/type/venue';
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
import { DeleteVenue } from './delete-venue';
import { GUID } from '@/modules/core/types/types';

export const DetailVenue: React.FC = () => {
  const { id } = useParams<{ id: GUID }>();
  const { venue, getVenueById } = useVenue();
  if (!id) {
    return null;
  }

  useEffect(() => {
    (async () => {
      await getVenueById(id);
    })();
  }, []);

  if (!venue) {
    return null;
  }
  return (
    <>
      <RenderVenueDetails {...venue} />
    </>
  );
};

const RenderVenueDetails: React.FC<IVenueResponse> = ({
  id,
  name,
  address,
  photoUrl,
}) => {
  const navigate = useNavigate();
  const [showPopupDelete, setShowPopupDelete] = useState<boolean>(false);
  const { deleteVenueById } = useVenue();

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
            Sede: {name}
          </Typography>

          <Stack direction="row" spacing={1}>
            <Tooltip title="Editar Sede">
              <IconButton color="primary" onClick={() => navigate(`editar`)}>
                <EditIcon />
              </IconButton>
            </Tooltip>

            <Tooltip title="Eliminar Sede">
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
          Dirección: {address}
        </Typography>
        {photoUrl && (
          <Typography
            variant="body2"
            color="primary"
            sx={{ mt: 1, wordBreak: 'break-word' }}
          >
            Foto: {photoUrl}
          </Typography>
        )}
      </CardContent>

      {showPopupDelete && (
        <DeleteVenue
          id={id}
          fn={deleteVenueById}
          onClose={() => setShowPopupDelete(false)}
        />
      )}
    </Card>
  );
};
