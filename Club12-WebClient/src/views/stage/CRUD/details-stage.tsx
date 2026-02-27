import { GUID } from '@/modules/core/types/types';
import { useStage } from '@/modules/stage/hook/stage.hook';
import { IStageContextProps, IStageResponse } from '@/modules/stage/type/stage';
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
import { DeleteStage } from './delete-stage';
import { InfoMatch } from '@/views/match/info';
import { InfoTeam } from '@/views/team/info';

export const DetailStage: React.FC = () => {
  const { stageId: id } = useParams<{ stageId: GUID }>();
  const { stage, getStageById } = useStage();
  useEffect(() => {
    if (id && (!stage || stage.id != id)) {
      (async () => {
        await getStageById(id);
      })();
    }
  }, [id]);
  return (
    <>
      {stage && (
        <>
          <RenderStageDetails {...stage} />
          <InfoMatch {...stage} />
          <InfoTeam stageId={id} />
        </>
      )}
    </>
  );
};

const RenderStageDetails: React.FC<IStageResponse> = ({ id, name }) => {
  const navigate = useNavigate();
  const [showPopupDelete, setShowPopupDelete] = useState<boolean>(false);
  const { deleteStagesById }: IStageContextProps = useStage();
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
            Etapa: {name}
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
        <DeleteStage
          id={id}
          fn={deleteStagesById}
          onClose={() => setShowPopupDelete(false)}
        />
      )}
    </Card>
  );
};
