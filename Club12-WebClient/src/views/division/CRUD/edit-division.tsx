import { GUID } from '@/modules/core/types/types';
import { useDivision } from '@/modules/division/hook/division.hook';
import { IPutDivisionRequest } from '@/modules/division/type/division';
import { useError } from '@/modules/error/hooks/error.hock';
import theme from '@/theme';
import { CustomBox } from '@/views/core/MUI/customsThemes/CustomBox';
import {
  Card,
  CardContent,
  Typography,
  TextField,
  Button,
  Checkbox,
  FormGroup,
  FormControlLabel,
} from '@mui/material';
import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';

export const EditDivision: React.FC = () => {
  const { division, putDivisionById, getDivisionsById } = useDivision();
  const { divisionId: id } = useParams<{
    divisionId: GUID;
  }>();

  useEffect(() => {
    if (id) {
      getDivisionsById(id);
    }
  }, [id]);

  const { errors, setMessage } = useError();
  const [editDivision, setEditDivision] = useState<IPutDivisionRequest>({
    name: '',
    isFinished: false,
  });

  useEffect(() => {
    if (division) {
      setEditDivision({ name: division.name, isFinished: division.isFinished });
    }
  }, [division]);

  const navigate = useNavigate();

  const handleCreate = async () => {
    if (!division || !id) {
      setMessage(400, ['Hubo un problema, intentelo mas tarde.']);
    }
    if (!editDivision.name.trim()) {
      setMessage(400, ['El nombre es obligatorio']);
      return;
    }
    const success = await putDivisionById(id as GUID, editDivision);
    if (success) {
      setMessage(200, ['División actualizada correctamente']);
      navigate(`/torneo/${division?.tournamentId}`);
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
            Editar Division
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
          <FormGroup>
            <FormControlLabel
              control={
                <Checkbox
                  checked={editDivision.isFinished}
                  onChange={e =>
                    setEditDivision({
                      ...editDivision,
                      isFinished: e.target.checked,
                    })
                  }
                />
              }
              label="¿Division Finalizada?"
            />
          </FormGroup>

          <TextField
            fullWidth
            label="Nombre"
            name="name"
            variant="outlined"
            margin="normal"
            value={editDivision.name}
            onChange={e =>
              setEditDivision({ ...editDivision, name: e.target.value })
            }
          />

          <Button
            fullWidth
            variant="contained"
            color="primary"
            onClick={handleCreate}
          >
            Guardar Cambios
          </Button>
        </CardContent>
      </Card>
    </CustomBox>
  );
};
