import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Card,
  CardContent,
  Typography,
  TextField,
  Button,
  Checkbox,
  FormControlLabel,
  FormGroup,
  useTheme,
} from '@mui/material';

import { CustomBox } from '@/views/core/MUI/customsThemes/CustomBox';
import { useStage } from '@/modules/stage/hook/stage.hook';
import { useError } from '@/modules/error/hooks/error.hock';
import { IErrorContextProp } from '@/modules/error/type/error';
import { IPutStageRequest } from '@/modules/stage/type/stage.d';
import { RoutesNavigationViews } from '@/views/core/routes-const';

export const EditStage: React.FC = () => {
  const theme = useTheme();
  const navigate = useNavigate();
  const { errors, setMessage }: IErrorContextProp = useError();

  const { stage, putStageById } = useStage();
  const [form, setForm] = useState<IPutStageRequest>({
    description: '',
    isActive: true,
  });

  if (!stage) {
    return (
      <CustomBox>
        <Typography color="text.secondary" align="center" mt={5}>
          Cargando datos de la etapa...
        </Typography>
      </CustomBox>
    );
  }

  const handleUpdate = async () => {
    if (form.description !== null && form.description?.trim() === '') {
      setMessage(400, ['La descripción no puede estar vacía']);
      return;
    }

    const res = await putStageById(stage.id, form);
    if (res) {
      navigate(`/${RoutesNavigationViews.Division}/${stage.divisionId}`);
    }
  };

  return (
    <CustomBox>
      <Card>
        <CardContent>
          <Typography
            variant="h4"
            gutterBottom
            align="center"
            color={theme.palette.primary.main}
          >
            Editar etapa: {stage.name}
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
            label="Descripción"
            variant="outlined"
            margin="normal"
            multiline
            rows={3}
            value={form.description ?? ''}
            onChange={e => setForm({ ...form, description: e.target.value })}
          />

          <FormGroup row sx={{ mt: 2, mb: 2 }}>
            <FormControlLabel
              control={
                <Checkbox
                  checked={form.isActive ?? true}
                  onChange={e =>
                    setForm({ ...form, isActive: e.target.checked })
                  }
                />
              }
              label="Activo"
            />
          </FormGroup>

          <Button
            fullWidth
            variant="contained"
            color="primary"
            onClick={handleUpdate}
          >
            Guardar Cambios
          </Button>
        </CardContent>
      </Card>
    </CustomBox>
  );
};
