import React, { useState } from 'react';
import { Bracket } from 'react-brackets';
import {  Box,  Paper,  Typography,  TextField,  Button,  Grid,  useTheme,} from '@mui/material';

const Bracket1: React.FC = () => {
  const theme = useTheme();
  const [numTeams, setNumTeams] = useState(4);
  const [teamInputs, setTeamInputs] = useState(
    Array(4)
      .fill('')
      .map((_, i) => `Team ${i + 1}`)
  );
  const [rounds, setRounds] = useState<
    { title: string; seeds: { id: number; teams: { name: string }[] }[] }[]
  >([]);


  const handleNumTeamsChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    let value = Number(event.target.value);
    if (value < 2) value = 2; // Mínimo 2 equipos
    //if (value % 2 !== 0) value += 1; // Debe ser par para emparejar
    setNumTeams(value);

    // Ajustar la lista de equipos con nombres predeterminados
    setTeamInputs(
      Array(value)
        .fill('')
        .map((_, i) => `Team ${i + 1}`)
    );
  };

  // Manejar cambios en los nombres de los equipos
  const handleTeamChange = (index: number, value: string) => {
    const updatedTeams = [...teamInputs];
    updatedTeams[index] = value;
    setTeamInputs(updatedTeams);
  };

  // Iniciar el torneo con los equipos ingresados
  const startTournament = () => {
    const newRounds = [
      {
        title: 'Round One',
        seeds: teamInputs.reduce(
          (acc, _team, index, array) => {
            if (index % 2 === 0) {
              acc.push({
                id: index / 2 + 1,
                teams: [{ name: array[index] }, { name: array[index + 1] }],
              });
            }
            return acc;
          },
          [] as { id: number; teams: { name: string }[] }[]
        ),
      },
    ];
    setRounds(newRounds);
  };

  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        minHeight: '100vh',
        backgroundColor: theme.palette.background.default,
        padding: 3,
      }}
    >
      <Paper
        elevation={4}
        sx={{
          padding: 3,
          borderRadius: 3,
          backgroundColor: theme.palette.background.paper,
        }}
      >
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            marginBottom: 2,
          }}
        >
          <Typography
            variant="h4"
            sx={{ fontWeight: 'bold', color: theme.palette.primary.main }}
          >
            🏆 Customizar torneo
          </Typography>

          <TextField
            type="number"
            label="Equipos"
            variant="outlined"
            value={numTeams}
            onChange={handleNumTeamsChange}
            inputProps={{ min: 2 }}
            sx={{ width: '100px' }}
          />
        </Box>

        {/* Formulario para ingresar los equipos */}
        <Grid container spacing={2}>
          {teamInputs.map((team, index) => (
            <Grid item xs={6} key={index}>
              <TextField
                fullWidth
                label={`Equipo ${index + 1}`}
                value={team}
                onChange={e => handleTeamChange(index, e.target.value)}
              />
            </Grid>
          ))}
        </Grid>

        <Button
          variant="contained"
          color="primary"
          sx={{ marginTop: 2 }}
          onClick={startTournament}
        >
          Crear Bracket
        </Button>
      </Paper>

      {rounds.length > 0 && (
        <Paper elevation={4} sx={{ marginTop: 4, padding: 3, borderRadius: 3 }}>
          <Typography
            variant="h5"
            sx={{ textAlign: 'center', marginBottom: 2 }}
          >
            Tournament Bracket
          </Typography>
          <Bracket rounds={rounds} />
        </Paper>
      )}
    </Box>
  );
};

export default Bracket1;
