import { GUID } from '@/modules/core/types/types';
import { useDivision } from '@/modules/division/hook/division.hook';
import {
  AddDivisionRequest,
  IDivisionContextProps,
  IDivisionResponse,
} from '@/modules/division/type/division';
import { useError } from '@/modules/error/hooks/error.hock';
import { IErrorContextProp } from '@/modules/error/type/error';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { CustomBox } from '@/views/core/MUI/customsThemes/CustomBox';
import { RoutesNavigationViews } from '@/views/core/routes-const';
import {
  Card,
  CardContent,
  Typography,
  TextField,
  Button,
} from '@mui/material';
import { useTheme } from '@mui/material/styles';
import React, { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';

export const CreateDivision: React.FC = () => {
  const theme = useTheme();
  const { tournamentId: id } = useParams<{ tournamentId: GUID }>();
  const { tournament } = useTournament();
  const { errors, setMessage }: IErrorContextProp = useError();
  const navigate = useNavigate();

  const { addDivision }: IDivisionContextProps = useDivision();

  if (!tournament) {
    navigate('/', { replace: true });
    return;
  }

  const [division, setDivision] = useState<AddDivisionRequest>({
    tournamentId: tournament.id,
    name: '',
  });

  const handleCreate = async () => {
    if (!division.name.trim()) {
      setMessage(400, ['El nombre es obligatorio']);
      return;
    }

    const res: IDivisionResponse | void = await addDivision(division);
    if (res) {
      navigate(`/${RoutesNavigationViews.Tournament}/${id}`);
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
            onChange={e => setDivision({ ...division, name: e.target.value })}
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
    </CustomBox>
  );
};
