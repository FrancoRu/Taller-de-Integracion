import { GUID } from '@/modules/core/types/types.d';
import { useError } from '@/modules/error/hooks/error.hock';
import { useMatch } from '@/modules/match/hook/match.hook';
import { IAddMatchRequest, TypeMatch } from '@/modules/match/type/match.d';
import { useStage } from '@/modules/stage/hook/stage.hook';
import { ITeamResponse } from '@/modules/team/type/team.d';
import { useVenue } from '@/modules/venue/hook/venue.hook';
import { GUID_EMPTY } from '@/views/core/constants/const';
import { CustomBox } from '@/views/core/MUI/customsThemes/CustomBox';
import { RoutesNavigationViews } from '@/views/core/routes-const';
import {
  Card,
  CardContent,
  Typography,
  TextField,
  MenuItem,
  Button,
} from '@mui/material';
import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';

export const CreateMatch: React.FC = () => {
  const { venues, getAllVenues } = useVenue();
  const { addMatch } = useMatch();
  const teams: ITeamResponse[] = MockTeams;

  const { stage } = useStage();
  const navigate = useNavigate();
  const { errors, setMessage } = useError();

  useEffect(() => {
    if (!stage) {
      navigate(`/${RoutesNavigationViews.Home}`, { replace: true });
      setMessage(400, [
        'Hubo un problema al cargar la información de creación de partido. Por favor, inténtelo más tarde.',
      ]);
    }
  }, [stage, navigate, setMessage]);

  useEffect(() => {
    if (!venues) (async () => await getAllVenues())();
  }, [venues, getAllVenues]);

  const [form, setForm] = useState<IAddMatchRequest>({
    matchDate: '',
    type: TypeMatch.Regular,
    homeTeamid: GUID_EMPTY,
    visitorTeamid: GUID_EMPTY,
    stageId: stage?.id as GUID,
    venueid: GUID_EMPTY,
  });

  const handleCreate = async () => {
    if (!form.matchDate) {
      setMessage(400, ['La fecha del partido es obligatoria.']);
      return;
    }
    if (form.homeTeamid === form.visitorTeamid) {
      setMessage(400, ['Los equipos no pueden ser el mismo.']);
      return;
    }

    const matchDate = new Date(form.matchDate);
    if (
      matchDate < new Date(stage!.startDate) ||
      matchDate > new Date(stage!.endDate)
    ) {
      setMessage(400, [
        'La fecha del partido debe estar dentro del rango de la etapa.',
      ]);
      return;
    }

    const res = await addMatch(form);
    if (res) navigate(`/${RoutesNavigationViews.Match}`);
  };

  const availableVisitorTeams = teams.filter(t => t.id !== form.homeTeamid);
  const availableHomeTeams = teams.filter(t => t.id !== form.visitorTeamid);

  return (
    <CustomBox>
      <Card>
        <CardContent>
          <Typography variant="h4" gutterBottom align="center">
            Crear Partido
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
            label="Fecha y Hora"
            name="matchDate"
            type="datetime-local"
            InputLabelProps={{ shrink: true }}
            variant="outlined"
            margin="normal"
            value={form.matchDate}
            onChange={e => setForm({ ...form, matchDate: e.target.value })}
            inputProps={{
              min: stage?.startDate?.slice(0, 16),
              max: stage?.endDate?.slice(0, 16),
            }}
          />

          <TextField
            fullWidth
            select
            label="Cancha"
            variant="outlined"
            margin="normal"
            value={form.venueid}
            onChange={e =>
              setForm({ ...form, venueid: e.target.value as GUID })
            }
          >
            {venues?.map(v => (
              <MenuItem key={v.id} value={v.id}>
                {v.name}
              </MenuItem>
            ))}
          </TextField>

          {/* Home Team */}
          <TextField
            fullWidth
            select
            label="Equipo Local"
            variant="outlined"
            margin="normal"
            value={form.homeTeamid}
            onChange={e =>
              setForm({ ...form, homeTeamid: e.target.value as GUID })
            }
          >
            {availableHomeTeams.map(team => (
              <MenuItem key={team.id} value={team.id}>
                {team.name}
              </MenuItem>
            ))}
          </TextField>

          {/* Visitor Team */}
          <TextField
            fullWidth
            select
            label="Equipo Visitante"
            variant="outlined"
            margin="normal"
            value={form.visitorTeamid}
            onChange={e =>
              setForm({ ...form, visitorTeamid: e.target.value as GUID })
            }
          >
            {availableVisitorTeams.map(team => (
              <MenuItem key={team.id} value={team.id}>
                {team.name}
              </MenuItem>
            ))}
          </TextField>

          <Button
            fullWidth
            variant="contained"
            color="primary"
            onClick={handleCreate}
            sx={{ mt: 2 }}
          >
            Crear Partido
          </Button>
        </CardContent>
      </Card>
    </CustomBox>
  );
};

const MockTeams: ITeamResponse[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    name: 'Tigres FC',
    threeLetterCode: 'TIG',
    shirtColor: 'Amarillo',
    logoUrl: 'https://fakeimg.pl/250x100/?text=Tigres',
    players: [],
  },
  {
    id: '22222222-2222-2222-2222-222222222222',
    name: 'Leones United',
    threeLetterCode: 'LEU',
    shirtColor: 'Rojo',
    logoUrl: 'https://fakeimg.pl/250x100/?text=Leones',
    players: [],
  },
  {
    id: '33333333-3333-3333-3333-333333333333',
    name: 'Águilas Doradas',
    threeLetterCode: 'AGD',
    shirtColor: 'Dorado',
    logoUrl: 'https://fakeimg.pl/250x100/?text=Aguilas',
    players: [],
  },
  {
    id: '44444444-4444-4444-4444-444444444444',
    name: 'Lobos Negros',
    threeLetterCode: 'LON',
    shirtColor: 'Negro',
    logoUrl: 'https://fakeimg.pl/250x100/?text=Lobos',
    players: [],
  },
  {
    id: '55555555-5555-5555-5555-555555555555',
    name: 'Panteras Blancas',
    threeLetterCode: 'PAB',
    shirtColor: 'Blanco',
    logoUrl: 'https://fakeimg.pl/250x100/?text=Panteras',
    players: [],
  },
  {
    id: '66666666-6666-6666-6666-666666666666',
    name: 'Toros Salvajes',
    threeLetterCode: 'TOS',
    shirtColor: 'Verde',
    logoUrl: 'https://fakeimg.pl/250x100/?text=Toros',
    players: [],
  },
  {
    id: '77777777-7777-7777-7777-777777777777',
    name: 'Jaguares FC',
    threeLetterCode: 'JAG',
    shirtColor: 'Naranja',
    logoUrl: 'https://fakeimg.pl/250x100/?text=Jaguares',
    players: [],
  },
  {
    id: '88888888-8888-8888-8888-888888888888',
    name: 'Pumas Rojos',
    threeLetterCode: 'PUR',
    shirtColor: 'Rojo Oscuro',
    logoUrl: 'https://fakeimg.pl/250x100/?text=Pumas',
    players: [],
  },
  {
    id: '99999999-9999-9999-9999-999999999999',
    name: 'Cóndores',
    threeLetterCode: 'CON',
    shirtColor: 'Azul',
    logoUrl: 'https://fakeimg.pl/250x100/?text=Condor',
    players: [],
  },
  {
    id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    name: 'Halcones',
    threeLetterCode: 'HAL',
    shirtColor: 'Gris',
    logoUrl: 'https://fakeimg.pl/250x100/?text=Halcones',
    players: [],
  },
];
