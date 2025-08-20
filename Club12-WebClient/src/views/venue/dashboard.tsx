import React, { useEffect, useState } from 'react';
import {
  Card,
  CardContent,
  Typography,
  Stack,
  Tooltip,
  IconButton,
  Box,
  Grid,
} from '@mui/material';
import { useNavigate } from 'react-router-dom';
import {
  ArrowForwardIcon,
  EditIcon,
  DeleteIcon,
} from '../core/MUI/icons/icons';
import { useVenue } from '@/modules/venue/hook/venue.hook';
import {
  IVenueContextProps,
  IVenueResponse,
} from '@/modules/venue/type/venue.d';
import LoadingIndicator from '../core/components/LoadingIndicator';
import { DeleteVenue } from './CRUD/delete-venue';
import { RoutesNavigationViews } from '../core/routes-const';
import { NoVenueMessage } from './NoVenueMessage';

export const VenueDashboard: React.FC = () => {
  const { venues, getAllVenues }: IVenueContextProps = useVenue();

  useEffect(() => {
    if (!venues || venues.length === 0) {
      (async () => {
        await getAllVenues();
      })();
    }
  }, [venues, getAllVenues]);

  return (
    <Box>
      {venues ? (
        venues.length > 0 ? (
          <Grid container spacing={3} sx={{ px: 2, py: 3 }}>
            {venues.map(v => (
              <Grid item key={v.id} xs={12} sm={8} md={4}>
                <RenderVenue {...v} />
              </Grid>
            ))}
          </Grid>
        ) : (
          <NoVenueMessage />
        )
      ) : (
        <LoadingIndicator />
      )}
    </Box>
  );
};

const RenderVenue: React.FC<IVenueResponse> = ({
  id,
  name,
  address,
  photoUrl,
}) => {
  const { deleteVenueById } = useVenue();
  const navigate = useNavigate();
  const [showPopup, setShowPopup] = useState(false);
  const [isAnimating, setIsAnimating] = useState(false);

  const handleNavigate = () => {
    setIsAnimating(true);
    setTimeout(() => {
      navigate(`/${RoutesNavigationViews.Venue}/${id}`);
      setIsAnimating(false);
    }, 300);
  };

  return (
    <Card
      sx={{
        backgroundColor: 'background.paper',
        border: '2px solid',
        borderColor: 'primary.main',
        transition: 'transform 0.2s',
        '&:hover': {
          transform: 'scale(1.02)',
        },
      }}
    >
      <CardContent>
        <Stack spacing={1} alignItems="center">
          <Typography variant="h6" align="center" color="text.primary">
            {name}
          </Typography>

          <Typography variant="body2" color="text.secondary" align="center">
            {address || 'Sin dirección'}
          </Typography>

          {photoUrl && (
            <img
              src={photoUrl}
              alt={name}
              style={{
                width: '100%',
                maxHeight: '150px',
                objectFit: 'contain',
                borderRadius: 4,
              }}
            />
          )}

          <Stack
            direction="row"
            spacing={1}
            justifyContent="center"
            sx={{ mt: 1 }}
          >
            <Tooltip title="Ver Sede">
              <span>
                <IconButton
                  color="primary"
                  disabled={isAnimating}
                  onClick={handleNavigate}
                  sx={{
                    transition: 'transform 0.5s ease',
                    transform: isAnimating
                      ? 'translateX(10px)'
                      : 'translateX(0)',
                  }}
                >
                  <ArrowForwardIcon titleAccess="Ver Sede" />
                </IconButton>
              </span>
            </Tooltip>

            <Tooltip title="Editar Sede">
              <IconButton
                color="secondary"
                onClick={() =>
                  navigate(`/${RoutesNavigationViews.Venue}/${id}/editar`)
                }
              >
                <EditIcon titleAccess="Editar Sede" />
              </IconButton>
            </Tooltip>

            <Tooltip title="Eliminar Sede">
              <IconButton color="error" onClick={() => setShowPopup(true)}>
                <DeleteIcon titleAccess="Eliminar Sede" />
              </IconButton>
            </Tooltip>
          </Stack>

          {showPopup && (
            <DeleteVenue
              id={id}
              route={RoutesNavigationViews.Home}
              fn={deleteVenueById}
              onClose={() => setShowPopup(false)}
            />
          )}
        </Stack>
      </CardContent>
    </Card>
  );
};
