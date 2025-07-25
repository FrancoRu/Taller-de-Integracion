import { GUID } from '@/modules/core/types/types';
import { useDivision } from '@/modules/division/hook/division.hook';
import { useError } from '@/modules/error/hooks/error.hock';
import { IErrorContextProp } from '@/modules/error/type/error';
import { useStage } from '@/modules/stage/hook/stage.hook';
import {
  IAddStageRequest,
  IStageContextProps,
  StageType,
} from '@/modules/stage/type/stage.d';
import {
  Card,
  CardContent,
  Typography,
  TextField,
  Button,
  useTheme,
} from '@mui/material';
import React, { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';

export const CreateStage: React.FC = () => {
  const theme = useTheme();
  const { id } = useParams<{ id: GUID }>();
  const { division } = useDivision();
  const { errors, setMessage }: IErrorContextProp = useError();
  const navigate = useNavigate();

  const { addStage }: IStageContextProps = useStage();

  if (!division) {
    navigate('/', { replace: true });
    return;
  }

  if (!id) return null;

  const [stage, setStage] = useState<IAddStageRequest>({
    divisionId: id,
    name: '',
    description: null,
    stageType: StageType.Group,
    isActive: false,
    isElimination: false,
    startDate: new Date(),
    endDate: new Date(),
  });

  const handleCreate = () => {
    if (!division.name.trim()) {
      setMessage(400, ['El nombre es obligatorio']);
      return;
    }

    addStage(stage);
    navigate(`/torneo/${id}`);
  };

  return (
    <Card>
      <CardContent>
        <Typography
          variant="h4"
          gutterBottom
          align="center"
          color={theme.palette.primary.main}
        >
          Crear Division
        </Typography>

        {errors && errors.length > 0 && (
          <>
            {errors.map((e, i) => (
              <Typography
                key={i}
                color="error"
                variant="body2"
                align="center"
                gutterBottom
              >
                {e}
              </Typography>
            ))}
          </>
        )}
        <TextField
          fullWidth
          label="Nombre"
          name="name"
          variant="outlined"
          margin="normal"
          value={division.name}
          onChange={e => setStage({ ...stage, name: e.target.value })}
        />

        <Button
          fullWidth
          variant="contained"
          color="primary"
          onClick={handleCreate}
        >
          Crear
        </Button>
      </CardContent>
    </Card>
  );
};
