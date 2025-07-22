import { GUID } from '@/modules/core/types/types';
import { useDivision } from '@/modules/division/hook/division.hook';
import {
  IDivisionContextProps,
  IDivisionResponse,
} from '@/modules/division/type/division';
import { CustomBox } from '@/views/core/MUI/customsThemes/CustomBox';
import { EditIcon, DeleteIcon, AddIcon } from '@/views/core/MUI/icons/icons';
import { RoutesNavigationViews } from '@/views/core/routes-const';
import { Fixture } from '@/views/division/common/fixture';
import { Positions } from '@/views/division/common/positions';
import { RenderPopupToDeleteTournament } from '@/views/tournament/CRUD/delete-tournament';
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
import { RenderPopupToDeleteDivision } from './delete-division';
import { IStageResponse } from '@/modules/stage/type/stage';
import { DivisionDashboard } from '../dashboard';

export const DetailDidivion: React.FC = () => {
  const { divisionId } = useParams<{ divisionId: GUID }>();
  const { division, getDivisionsById } = useDivision();

  if (!divisionId) {
    return null;
  }

  useEffect(() => {
    (async () => {
      await getDivisionsById(divisionId);
    })();
  }, []);

  if (!division) {
    return null;
  }

  return (
    <>
      <RenderDivisionDetails {...division} />
      <Card
        sx={{
          width: '98%',
          mx: 'auto',
          px: { xs: 2, sm: 3, md: 4 },
        }}
      >
        <CardContent>
          <Typography variant="h6">
            Total de Fechas en la division: {division.stages?.length ?? 0}
          </Typography>
          <NoStagesMessage></NoStagesMessage>
          {/* <DivisionDashboard /> */}
        </CardContent>
      </Card>
      <Outlet />
    </>
  );
};

const NoStagesMessage: React.FC = () => (
  <CustomBox>
    <Typography>
      No se encontraron fechas cargadas para esta division todavía
    </Typography>
  </CustomBox>
);

const RenderDivisionDetails: React.FC<IDivisionResponse> = ({ id, name }) => {
  const navigate = useNavigate();
  const [showPopup, setShowPopup] = useState<boolean>(false);
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
            Division: {name}
          </Typography>

          <Stack direction="row" spacing={1}>
            <Tooltip title="Editar Division">
              <IconButton
                color="primary"
                onClick={() =>
                  navigate(`/${RoutesNavigationViews.Division}/${id}/editar`)
                }
              >
                <EditIcon />
              </IconButton>
            </Tooltip>

            <Tooltip title="Eliminar Division">
              <IconButton color="error" onClick={() => setShowPopup(true)}>
                <DeleteIcon />
              </IconButton>
            </Tooltip>

            <Tooltip title="Agregar Fecha">
              <IconButton
                color="success"
                onClick={() =>
                  navigate(`/${RoutesNavigationViews.Stage}/crear`)
                }
              >
                <AddIcon />
              </IconButton>
            </Tooltip>
          </Stack>
        </Stack>
      </CardContent>

      {showPopup && (
        <RenderPopupToDeleteDivision
          id={id}
          fn={deleteDivisionsById}
          onClose={() => setShowPopup(false)}
        />
      )}
    </Card>
    // <Card>
    //   <CardContent>
    //     {!stages || stages.length === 0 ? (
    //       <NoStagesMessage />
    //     ) : (
    //       <>
    //         <Fixture stages={stages} />
    //         <Positions stages={stages} />
    //       </>
    //     )}
    //   </CardContent>
    // </Card>
  );
};
