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
  Box,
} from '@mui/material';
import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { DeleteVenue } from './delete-venue';
import { GUID } from '@/modules/core/types/types';
import { RoutesNavigationViews } from '@/views/core/routes-const';

export const DetailVenue: React.FC = () => {
  const { venueId: id } = useParams<{ venueId: GUID }>();
  const { venue, getVenueById } = useVenue();

  useEffect(() => {
    if (id) {
      (async () => {
        await getVenueById(id);
      })();
    }
  }, [id]);

  if (!venue) return null;

  return <RenderVenueDetails {...venue} />;
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

        <Stack
          direction="row"
          spacing={2}
          alignItems="center"
          justifyContent="space-between"
        >
          {photoUrl && (
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
                src={photoUrl}
                alt={`Foto de la sede ${name}`}
                style={{ width: '100%', height: '100%', objectFit: 'cover' }}
              />
            </Box>
          )}

          <Box sx={{ flexGrow: 1 }}>
            <Typography variant="body2" color="text.secondary" gutterBottom>
              Dirección: {address}
            </Typography>
          </Box>
        </Stack>
      </CardContent>

      {showPopupDelete && (
        <DeleteVenue
          id={id}
          route={RoutesNavigationViews.Venue}
          fn={deleteVenueById}
          onClose={() => setShowPopupDelete(false)}
        />
      )}
    </Card>
  );
};
