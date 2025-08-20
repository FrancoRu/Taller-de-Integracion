import React, { useEffect, useState } from 'react';
import { useError } from '@/modules/error/hooks/error.hock';
import { IErrorContextProp } from '@/modules/error/type/error';
import { CustomBox } from '@/views/core/MUI/customsThemes/CustomBox';
import {
  Card,
  CardContent,
  Typography,
  IconButton,
  Stack,
  Box,
  Button,
  MenuItem,
  TextField,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import DeleteIcon from '@mui/icons-material/Delete';
import { useTheme } from '@mui/material/styles';
import { ITeamContextProps, ITeamResponse } from '@/modules/team/type/team';
import { useTeam } from '@/modules/team/hook/team.hook';
import { GenericResponsePagination, GUID } from '@/modules/core/types/types';
import { ITournamentContextProps } from '@/modules/tournament/type/tournament';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { useNavigate, useParams } from 'react-router-dom';
import { GUID_EMPTY } from '@/views/core/constants/const';
import { RoutesNavigationViews } from '@/views/core/routes-const';

export const RegisterTeamsTournament: React.FC = () => {
  const theme = useTheme();
  const { tournamentId: id } = useParams<{ tournamentId: GUID }>();
  const { errors }: IErrorContextProp = useError();
  const { getTeamsByFiltered }: ITeamContextProps = useTeam();
  const { registerTeamsByTournamentId }: ITournamentContextProps =
    useTournament();
  const [teamsId, setTeams] = useState<GUID[]>([GUID_EMPTY]);
  const [availableTeams, setAvailableTeams] = useState<ITeamResponse[]>([]);
  const navigate = useNavigate();
  useEffect(() => {
    (async () => {
      const res: GenericResponsePagination<ITeamResponse> | void =
        await getTeamsByFiltered({});
      if (res) {
        setAvailableTeams(res.items);

        const preselected = res.items
          .filter(team => team.tournamentId === id)
          .map(team => team.id);

        setTeams(preselected.length > 0 ? preselected : [GUID_EMPTY]);
      }
    })();
  }, [getTeamsByFiltered]);

  const handleAddTeam = () => {
    setTeams([...teamsId, GUID_EMPTY]);
  };

  const handleChange = (index: number, teamId: GUID) => {
    const updatedTeams = [...teamsId];
    updatedTeams[index] = teamId;
    setTeams(updatedTeams);
  };

  const handleDelete = (index: number) => {
    const updatedTeams = teamsId.filter((_, i) => i !== index);
    setTeams(updatedTeams.length ? updatedTeams : []);
  };

  const handleSave = async () => {
    if (id) {
      const res: boolean | void = await registerTeamsByTournamentId(
        id,
        teamsId.filter(e => e !== GUID_EMPTY)
      );

      if (res) {
        navigate(`/${RoutesNavigationViews.Tournament}/${id}`, {
          replace: true,
        });
        return;
      }
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
            Registrar equipos en el torneo
          </Typography>

          {errors?.map((e, i) => (
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

          <Stack spacing={2} mt={2}>
            {teamsId.map((teamId, index) => (
              <Box key={index} display="flex" alignItems="center" gap={1}>
                <TextField
                  fullWidth
                  select
                  label={`Equipo ${index + 1}`}
                  value={teamId}
                  onChange={e => handleChange(index, e.target.value as GUID)}
                >
                  {availableTeams
                    .filter(
                      team => !teamsId.includes(team.id) || team.id === teamId
                    )
                    .map(team => (
                      <MenuItem key={team.id} value={team.id}>
                        {team.name}
                      </MenuItem>
                    ))}
                </TextField>

                <IconButton
                  color="error"
                  onClick={() => handleDelete(index)}
                  aria-label="Eliminar equipo"
                >
                  <DeleteIcon />
                </IconButton>

                {index === teamsId.length - 1 && (
                  <IconButton
                    color="primary"
                    onClick={handleAddTeam}
                    disabled={teamId === GUID_EMPTY}
                    aria-label="Agregar equipo"
                  >
                    <AddIcon />
                  </IconButton>
                )}
              </Box>
            ))}
          </Stack>

          <Button
            fullWidth
            variant="contained"
            color="primary"
            onClick={handleSave}
            sx={{ mt: 2 }}
          >
            Guardar Cambios
          </Button>
        </CardContent>
      </Card>
    </CustomBox>
  );
};
