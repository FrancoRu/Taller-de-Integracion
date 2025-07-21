import { useDivision } from '@/modules/division/hook/division.hook';
import {
  AddDivisionRequest,
  IDivisionContextProps,
} from '@/modules/division/type/division';
import { useError } from '@/modules/error/hooks/error.hock';
import { IErrorContextProp } from '@/modules/error/type/error';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import {
  Card,
  CardContent,
  Typography,
  TextField,
  Button,
} from '@mui/material';
import { useTheme } from '@mui/material/styles';
import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';

export const CreateDivision: React.FC = () => {
  const theme = useTheme();
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

  const handleCreate = () => {
    if (!division.name.trim()) {
      setMessage(400, ['El nombre es obligatorio']);
      return;
    }

    addDivision(division);
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
  );
};
