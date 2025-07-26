import { translateStageType } from '@/modules/core/utils/translateStageType';
import { useDivision } from '@/modules/division/hook/division.hook';
import { useError } from '@/modules/error/hooks/error.hock';
import { IErrorContextProp } from '@/modules/error/type/error';
import { useStage } from '@/modules/stage/hook/stage.hook';
import {
  IAddStageRequest,
  IStageContextProps,
  IStageResponse,
  StageType,
} from '@/modules/stage/type/stage.d';
import { CustomBox } from '@/views/core/MUI/customsThemes/CustomBox';
import { RoutesNavigationViews } from '@/views/core/routes-const';
import {
  Card,
  CardContent,
  Typography,
  TextField,
  Button,
  useTheme,
  Checkbox,
  FormControlLabel,
  FormGroup,
  MenuItem,
} from '@mui/material';
import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';

export const CreateStage: React.FC = () => {
  const theme = useTheme();
  const { division } = useDivision();
  const { errors, setMessage }: IErrorContextProp = useError();
  const navigate = useNavigate();

  const { addStage }: IStageContextProps = useStage();

  if (!division) {
    navigate('/', { replace: true });
    return;
  }

  const [stage, setStage] = useState<IAddStageRequest>({
    divisionId: division.id,
    name: '',
    description: null,
    stageType: StageType.Group,
    isActive: true,
    isElimination: false,
    startDate: new Date(),
    endDate: new Date(),
  });

  const handleCreate = async () => {
    if (!stage.name.trim()) {
      setMessage(400, ['El nombre es obligatorio']);
      return;
    }

    if (stage.endDate <= stage.startDate) {
      setMessage(400, [
        'La fecha de fin no puede ser menor o igual que la fecha de inicio',
      ]);
      return;
    }

    const res: IStageResponse | void = await addStage(stage);

    if (res) {
      navigate(`/${RoutesNavigationViews.Division}/${division.id}`);
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
            Crear etapa
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
            value={stage.name}
            onChange={e => setStage({ ...stage, name: e.target.value })}
          />

          <TextField
            fullWidth
            label="Descripción"
            name="description"
            variant="outlined"
            margin="normal"
            multiline
            rows={3}
            value={stage.description ?? ''}
            onChange={e => setStage({ ...stage, description: e.target.value })}
          />

          <TextField
            fullWidth
            select
            label="Tipo de etapa"
            name="stageType"
            variant="outlined"
            margin="normal"
            value={stage.stageType}
            onChange={e =>
              setStage({ ...stage, stageType: e.target.value as StageType })
            }
          >
            {Object.values(StageType).map(type => (
              <MenuItem key={type} value={type}>
                {translateStageType(type)}
              </MenuItem>
            ))}
          </TextField>

          <TextField
            fullWidth
            label="Fecha de inicio"
            type="date"
            margin="normal"
            InputLabelProps={{ shrink: true }}
            value={stage.startDate.toISOString().split('T')[0]}
            onChange={e =>
              setStage({ ...stage, startDate: new Date(e.target.value) })
            }
          />
          <TextField
            fullWidth
            label="Fecha de fin"
            type="date"
            margin="normal"
            InputLabelProps={{ shrink: true }}
            value={stage.endDate.toISOString().split('T')[0]}
            onChange={e =>
              setStage({ ...stage, endDate: new Date(e.target.value) })
            }
          />

          <FormGroup row sx={{ mt: 2, mb: 2 }}>
            <FormControlLabel
              control={
                <Checkbox
                  checked={stage.isActive ?? true}
                  onChange={e =>
                    setStage({ ...stage, isActive: e.target.checked })
                  }
                />
              }
              label="Activo"
            />
            {stage.stageType !== StageType.Group && (
              <FormControlLabel
                control={
                  <Checkbox
                    checked={stage.isElimination ?? false}
                    onChange={e =>
                      setStage({ ...stage, isElimination: e.target.checked })
                    }
                  />
                }
                label="Eliminación"
              />
            )}
          </FormGroup>

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
    </CustomBox>
  );
};
